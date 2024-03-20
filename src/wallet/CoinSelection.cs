namespace chia.dotnet.wallet;

/// <summary>
/// Represents the different strategies for selecting coins.
/// </summary>
public enum CoinSelection
{
    /// <summary>
    /// Select the smallest coins first.
    /// </summary>
    Smallest,

    /// <summary>
    /// Select the largest coins first.
    /// </summary>
    Largest,

    /// <summary>
    /// Select the newest coins first.
    /// </summary>
    Newest,

    /// <summary>
    /// Select the oldest coins first.
    /// </summary>
    Oldest
}