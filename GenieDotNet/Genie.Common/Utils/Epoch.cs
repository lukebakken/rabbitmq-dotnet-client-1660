using Google.Protobuf.WellKnownTypes;

namespace Genie.Common.Utils;

public class Epoch
{
    public static DateTime? Convert(Timestamp stamp)
    {
        if (stamp == null) return null;

        return stamp.ToDateTime();
    }

    public static Timestamp? Convert(DateTime? date)
    {
        if (date == null) return null;

        return Timestamp.FromDateTime(date.Value);
    }

    public static long? ToEpoch(DateTime? date)
    {
        if (date == null) return null;

        var ds = new DateTimeOffset(date.Value);
        return ds.ToUnixTimeSeconds();
    }
}