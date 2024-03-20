namespace chia.dotnet.wallet;

/// <summary>
/// Represents the options for a wallet.
/// </summary>
public class WalletOptions
{
    /// <summary>
    /// Gets or sets the minimum address count.
    /// </summary>
    public int MinAddressCount { get; init; }

    /// <summary>
    /// Gets or sets the maximum address count.
    /// </summary>
    public int MaxAddressCount { get; init; }

    /// <summary>
    /// Gets or sets the count of unused addresses.
    /// </summary>
    public int UnusedAddressCount { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether instant coin records are enabled.
    /// </summary>
    public bool InstantCoinRecords { get; init; }

    /// <summary>
    /// Gets the default wallet options.
    /// </summary>
    public static readonly WalletOptions DefaultWalletOptions = new()
    {
        MinAddressCount = 50,
        MaxAddressCount = int.MaxValue,
        UnusedAddressCount = 10,
        InstantCoinRecords = true
    };
}