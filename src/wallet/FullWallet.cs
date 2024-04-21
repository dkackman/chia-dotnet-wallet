using chia.dotnet.bls;
using chia.dotnet.clvm;
using System.Numerics;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents an abstract wallet class that provides common functionality for different types of wallets.
/// </summary>
public class FullWallet(FullNodeProxy node, WalletProxy wallet, KeyStore keyStore) : IWallet
{
    private readonly FullNodeProxy _fullNode = node;
    private readonly Wallet _xchWallet = new(1, wallet);
    private readonly KeyStore _keyStore = keyStore;

    /// <summary>
    /// Gets the hidden puzzle hash.
    /// </summary>
    public byte[] HiddenPuzzleHash { get; init; } = Puzzles.GetPuzzle("defaultHidden").Hash();

    /// <summary>
    /// Finds a puzzle by hash.
    /// </summary>
    /// <param name="puzzleHash"></param>
    /// <returns></returns>
    public Program FindProgram(string puzzleHash) => throw new NotImplementedException();

    /// <summary>
    /// Indicates whether the provided program is ours.
    /// </summary>
    /// <param name="revealProgram"></param>
    /// <returns></returns>
    public bool IsOurs(Program revealProgram) => throw new NotImplementedException();

    /// <summary>
    /// Gets the balance of the wallet.
    /// </summary>
    /// <returns>The balance</returns>
    public async Task<BigInteger> GetBalance()
    {
        var balance = await _xchWallet.GetBalance();
        return balance.ConfirmedWalletBalance;
    }

    /// <summary>
    /// Waits for the wallet to sync.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task WaitForSync(CancellationToken cancellationToken = default) => await _xchWallet.WalletProxy.WaitForSync(cancellationToken: cancellationToken);

    /// <summary>
    /// Selects coin records for spending.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="coinSelection"></param>
    /// <param name="minimumCoinRecords"></param>
    /// <param name="required"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception">Throws when insufficient funds or no viable coins found</exception>
    public async Task<List<CoinRecord>> SelectCoinRecords(BigInteger amount, CoinSelection coinSelection, int minimumCoinRecords = 0, bool required = true, CancellationToken cancellationToken = default)
    {
        var spendableCoins = await _xchWallet.GetSpendableCoins(null, null, cancellationToken: cancellationToken);
        var coinRecords = spendableCoins.ConfirmedRecords;

        var viableCoinRecords = coinRecords.Where(coinRecord => coinRecord.SpentBlockIndex <= 0 || !coinRecord.Spent).ToList();

        switch (coinSelection)
        {
            case CoinSelection.Smallest:
                viableCoinRecords = [.. viableCoinRecords.OrderBy(a => a.Coin.Amount)];
                break;
            case CoinSelection.Largest:
                viableCoinRecords = [.. viableCoinRecords.OrderByDescending(a => a.Coin.Amount)];
                break;
            case CoinSelection.Newest:
                viableCoinRecords = [.. viableCoinRecords.OrderByDescending(a => a.Timestamp)];
                break;
            case CoinSelection.Oldest:
                viableCoinRecords = [.. viableCoinRecords.OrderBy(a => a.Timestamp)];
                break;
        }

        var selectedCoinRecords = new List<CoinRecord>();

        BigInteger totalAmount = 0;

        for (int i = 0; (totalAmount < amount || selectedCoinRecords.Count < minimumCoinRecords) && i < viableCoinRecords.Count; i++)
        {
            var coinRecord = viableCoinRecords[i];
            selectedCoinRecords.Add(coinRecord);
            totalAmount += coinRecord.Coin.Amount;
        }

        if (selectedCoinRecords.Count < minimumCoinRecords)
            throw new Exception("Insufficient number of coin records.");

        if (totalAmount < amount && required)
            throw new Exception("Insufficient funds.");

        return selectedCoinRecords;
    }

    /// <summary>
    /// Completes the spend of the given spend bundle.
    /// </summary>
    /// <param name="spendBundle"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task CompleteSpend(SpendBundle spendBundle, CancellationToken cancellationToken = default)
    {
        var result = await _fullNode.PushTx(spendBundle, cancellationToken);

        if (!result)
        {
            throw new Exception("Could not push transaction.");
        }
    }

    /// <summary>
    /// Signs a spend bundle with the specified aggregated signature me extra data.
    /// </summary>
    /// <param name="spendBundle">The spend bundle to sign.</param>
    /// <param name="aggSigMeExtraData">The aggregated signature me extra data.</param>
    /// <returns>The signed spend bundle.</returns>
    public SpendBundle SignSpend(SpendBundle spendBundle, byte[] aggSigMeExtraData)
    {
        var syntheticPrivateKeys = _keyStore.Keys
            .Where(keyPair => keyPair.PrivateKey != null)
            .Select(keyPair => KeyDerivation.CalculateSyntheticPrivateKey(keyPair.PrivateKey!.Value, HiddenPuzzleHash))
            .ToList();

        if (_keyStore.PrivateKey != null)
        {
            syntheticPrivateKeys.Add(_keyStore.PrivateKey.Value);
        }

        var privateKeys = _keyStore.Keys
            .Where(keyPair => keyPair.PrivateKey != null)
            .Select(item => item.PrivateKey!.Value);

        syntheticPrivateKeys.AddRange(privateKeys!);

        return Sign.SignSpendBundle(spendBundle, aggSigMeExtraData, true, [.. syntheticPrivateKeys]);
    }
}