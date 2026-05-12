using AssettoServer.Network.ClientMessages;

namespace LeaderboardPlugin.Packets;

[OnlineEvent(Key = "AS_LB_LapInfo")]
public class LapInfoPacket : OnlineEvent<LapInfoPacket>
{
    [OnlineEventField(Name = "absLevel")]
    public byte AbsLevel;

    [OnlineEventField(Name = "tcLevel")]
    public byte TcLevel;

    [OnlineEventField(Name = "stabilityControl")]
    public float StabilityControl;

    [OnlineEventField(Name = "autoShifting")]
    public bool AutoShifting;

    [OnlineEventField(Name = "inputMethod")]
    public byte InputMethod;

    [OnlineEventField(Name = "tyreCompound")]
    public byte TyreCompound;
}
