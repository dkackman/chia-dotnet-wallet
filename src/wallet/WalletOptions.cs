namespace chia.dotnet.wallet;

public class WalletOptions
{
    public int MinAddressCount { get; init; }
    public int MaxAddressCount { get; init; }
    public int UnusedAddressCount { get; init; }
    public bool InstantCoinRecords { get; init; }

    public static readonly WalletOptions DefaultWalletOptions = new()
    {
        MinAddressCount = 50,
        MaxAddressCount = int.MaxValue,
        UnusedAddressCount = 10,
        InstantCoinRecords = true
    };
}