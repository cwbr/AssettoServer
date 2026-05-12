using System.Text.Json.Serialization;

namespace LeaderboardPlugin;

/// <summary>
/// JSON payload matching the leaderboard API contract: POST /api/v1/laps
/// </summary>
public class LapPayload
{
    [JsonPropertyName("game_slug")]
    public string GameSlug { get; set; } = "";

    [JsonPropertyName("player_platform")]
    public string PlayerPlatform { get; set; } = "";

    [JsonPropertyName("player_id")]
    public string PlayerId { get; set; } = "";

    [JsonPropertyName("player_name")]
    public string PlayerName { get; set; } = "";

    [JsonPropertyName("player_country")]
    public string PlayerCountry { get; set; } = "";

    [JsonPropertyName("track")]
    public string Track { get; set; } = "";

    [JsonPropertyName("track_config")]
    public string TrackConfig { get; set; } = "";

    [JsonPropertyName("car")]
    public string Car { get; set; } = "";

    [JsonPropertyName("car_name")]
    public string? CarName { get; set; }

    [JsonPropertyName("car_class")]
    public string? CarClass { get; set; }

    [JsonPropertyName("lap_time_ms")]
    public uint LapTimeMs { get; set; }

    [JsonPropertyName("sectors_ms")]
    public uint[]? SectorsMs { get; set; }

    [JsonPropertyName("cuts")]
    public int Cuts { get; set; }

    [JsonPropertyName("valid")]
    public bool Valid { get; set; }

    [JsonPropertyName("grip")]
    public float Grip { get; set; }

    [JsonPropertyName("session_type")]
    public string SessionType { get; set; } = "";

    [JsonPropertyName("abs_level")]
    public byte? AbsLevel { get; set; }

    [JsonPropertyName("tc_level")]
    public byte? TcLevel { get; set; }

    [JsonPropertyName("stability_control")]
    public float? StabilityControl { get; set; }

    [JsonPropertyName("auto_shifting")]
    public bool? AutoShifting { get; set; }

    [JsonPropertyName("input_method")]
    public byte? InputMethod { get; set; }

    [JsonPropertyName("tyre_compound")]
    public byte? TyreCompound { get; set; }
}
