using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Channels;
using AssettoServer.Network.Tcp;
using AssettoServer.Server;
using AssettoServer.Server.Configuration;
using AssettoServer.Shared.Network.Packets.Outgoing;
using LeaderboardPlugin.Packets;
using Microsoft.Extensions.Hosting;
using Serilog;


namespace LeaderboardPlugin;

public class Leaderboard : BackgroundService
{
    private readonly LeaderboardConfiguration _config;
    private readonly EntryCarManager _entryCarManager;
    private readonly ACServerConfiguration _serverConfig;
    private readonly SessionManager _sessionManager;
    private readonly HttpClient _httpClient;

    // Track sector splits per session slot until the lap completes
    private readonly ConcurrentDictionary<byte, List<uint>> _pendingSectors = new();

    // Last known lap info state per session slot (populated by CSP clients only)
    private readonly ConcurrentDictionary<byte, LapInfoPacket> _lastLapInfo = new();

    // Outbound queue so we never block the game thread
    private readonly Channel<LapPayload> _sendQueue = Channel.CreateBounded<LapPayload>(
        new BoundedChannelOptions(512)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public Leaderboard(
        LeaderboardConfiguration config,
        EntryCarManager entryCarManager,
        ACServerConfiguration serverConfig,
        SessionManager sessionManager,
        HttpClient httpClient,
        CSPServerScriptProvider scriptProvider,
        CSPClientMessageTypeManager cspClientMessageTypeManager)
    {
        _config = config;
        _entryCarManager = entryCarManager;
        _serverConfig = serverConfig;
        _sessionManager = sessionManager;
        _httpClient = httpClient;

        _httpClient.BaseAddress = new Uri(_config.ApiUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        _httpClient.Timeout = TimeSpan.FromSeconds(10);

        // Register CSP Lua script (delivers to CSP clients; non-CSP clients simply ignore it)
        var luaPath = Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lua", "leaderboard.lua");
        if (File.Exists(luaPath))
        {
            using var reader = new StreamReader(luaPath);
            scriptProvider.AddScript(reader.ReadToEnd(), "leaderboard.lua");
            cspClientMessageTypeManager.RegisterOnlineEvent<LapInfoPacket>(OnLapInfoPacket);
            Log.Debug("LeaderboardPlugin: CSP script registered for assist/input reporting");
        }
    }

    private void OnLapInfoPacket(ACTcpClient client, LapInfoPacket packet)
    {
        _lastLapInfo[client.SessionId] = packet;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _entryCarManager.ClientConnected += OnClientConnected;
        _entryCarManager.ClientDisconnected += OnClientDisconnected;

        Log.Information("LeaderboardPlugin started — sending laps to {Url}", _config.ApiUrl);

        // Process the send queue
        await foreach (var payload in _sendQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/v1/laps", payload, stoppingToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(stoppingToken);
                    Log.Warning("Leaderboard API returned {Status}: {Body}", response.StatusCode, body);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Error(ex, "Failed to send lap to leaderboard API");
            }
        }
    }

    private void OnClientConnected(ACTcpClient sender, EventArgs args)
    {
        sender.LapCompleted += OnLapCompleted;
        if (_config.IncludeSectors)
        {
            sender.SectorSplit += OnSectorSplit;
        }
        _pendingSectors[sender.SessionId] = new List<uint>();

        Log.Debug("LeaderboardPlugin: tracking laps for {Name} (slot {SessionId})",
            sender.Name, sender.SessionId);
    }

    private void OnClientDisconnected(ACTcpClient sender, EventArgs args)
    {
        sender.LapCompleted -= OnLapCompleted;
        sender.SectorSplit -= OnSectorSplit;
        _pendingSectors.TryRemove(sender.SessionId, out _);
        _lastLapInfo.TryRemove(sender.SessionId, out _);
    }

    private void OnSectorSplit(ACTcpClient sender, SectorSplitEventArgs args)
    {
        if (!_pendingSectors.TryGetValue(sender.SessionId, out var sectors))
            return;

        // Ensure we store in order even if events arrive slightly out of order
        while (sectors.Count <= args.Packet.SplitIndex)
            sectors.Add(0);

        sectors[args.Packet.SplitIndex] = args.Packet.SplitTime;
    }

    private void OnLapCompleted(ACTcpClient sender, LapCompletedEventArgs args)
    {
        var lapTime = args.Packet.LapTime;
        var cuts = args.Packet.Cuts;

        Log.Debug("LeaderboardPlugin: raw lap event from {Name} — {LapTime}ms, cuts={Cuts}",
            sender.Name, lapTime, cuts);

        // Filter obviously invalid laps
        if (lapTime < _config.MinLapTimeSec * 1000)
        {
            Log.Debug("LeaderboardPlugin: discarding lap from {Name} — {LapTime}ms below minimum {Min}ms",
                sender.Name, lapTime, _config.MinLapTimeSec * 1000);
            return;
        }

        bool valid = cuts == 0;
        if (_config.ValidLapsOnly && !valid)
        {
            Log.Debug("LeaderboardPlugin: discarding invalid lap from {Name} — {Cuts} cuts",
                sender.Name, cuts);
            return;
        }

        // Collect sectors and reset for next lap
        uint[]? sectors = null;
        if (_pendingSectors.TryGetValue(sender.SessionId, out var sectorList) && sectorList.Count > 0)
        {
            sectors = sectorList.ToArray();
            sectorList.Clear();
        }

        var sessionType = _sessionManager.CurrentSession.Configuration.Type.ToString().ToLowerInvariant();

        // Grab assist/input state if reported by CSP client (null for non-CSP players)
        byte? absLevel = null;
        byte? tcLevel = null;
        float? stabilityControl = null;
        bool? autoShifting = null;
        byte? inputMethod = null;
        byte? tyreCompound = null;
        if (_lastLapInfo.TryGetValue(sender.SessionId, out var lapInfo))
        {
            absLevel = lapInfo.AbsLevel;
            tcLevel = lapInfo.TcLevel;
            stabilityControl = lapInfo.StabilityControl;
            autoShifting = lapInfo.AutoShifting;
            inputMethod = lapInfo.InputMethod;
            tyreCompound = lapInfo.TyreCompound;
        }

        var payload = new LapPayload
        {
            GameSlug = "assetto-corsa",
            PlayerPlatform = "steam",
            PlayerId = sender.Guid.ToString(),
            PlayerName = sender.Name ?? "Unknown",
            PlayerCountry = sender.NationCode ?? "",
            Track = _serverConfig.Server.Track,
            TrackConfig = _serverConfig.Server.TrackConfig,
            Car = sender.EntryCar.Model,
            LapTimeMs = lapTime,
            SectorsMs = sectors,
            Cuts = cuts,
            Valid = valid,
            Grip = args.Packet.TrackGrip,
            SessionType = sessionType,
            AbsLevel = absLevel,
            TcLevel = tcLevel,
            StabilityControl = stabilityControl,
            AutoShifting = autoShifting,
            InputMethod = inputMethod,
            TyreCompound = tyreCompound
        };

        Log.Information("LeaderboardPlugin: lap completed by {Name} — {LapTime}ms, cuts={Cuts}, valid={Valid}, car={Car}, track={Track}",
            sender.Name, lapTime, cuts, valid, sender.EntryCar.Model, _serverConfig.Server.Track);

        // Non-blocking enqueue
        if (!_sendQueue.Writer.TryWrite(payload))
        {
            Log.Warning("LeaderboardPlugin: send queue full, dropping lap");
        }
    }
}
