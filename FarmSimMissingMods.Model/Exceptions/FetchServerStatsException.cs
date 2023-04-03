using System.Runtime.Serialization;

namespace FarmSimMissingMods.Model.Exceptions;

public class FetchServerStatsException : Exception
{
    public FetchServerStatsException()
    {
    }

    protected FetchServerStatsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public FetchServerStatsException(string? message) : base(message)
    {
    }

    public FetchServerStatsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}