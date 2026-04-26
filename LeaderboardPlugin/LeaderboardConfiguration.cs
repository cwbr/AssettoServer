using AssettoServer.Server.Configuration;
using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace LeaderboardPlugin;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public class LeaderboardConfiguration : IValidateConfiguration<LeaderboardConfigurationValidator>
{
    [YamlMember(Description = "URL of the leaderboard backend API (e.g. http://yourserver:8080)")]
    public string ApiUrl { get; init; } = "http://localhost:8080";

    [YamlMember(Description = "API key for this server (generated in the leaderboard admin)")]
    public string ApiKey { get; init; } = "";

    [YamlMember(Description = "Only submit laps with 0 cuts")]
    public bool ValidLapsOnly { get; init; } = true;

    [YamlMember(Description = "Include sector split times")]
    public bool IncludeSectors { get; init; } = true;

    [YamlMember(Description = "Minimum lap time in seconds to accept (filters out reset/teleport laps)")]
    public uint MinLapTimeSec { get; init; } = 10;
}
