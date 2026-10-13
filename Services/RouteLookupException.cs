namespace PostcodeDistanceCalculator.Services;

/// <summary>
/// An expected failure while looking up postcodes or routes (unknown postcode, no route, network error,
/// service down). The message is written for the user. Anything else thrown during a calculation is a bug.
/// </summary>
public sealed class RouteLookupException : Exception
{
    public RouteLookupException(string message) : base(message) { }

    public RouteLookupException(string message, Exception innerException) : base(message, innerException) { }
}
