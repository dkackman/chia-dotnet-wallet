using System.Numerics;
using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents a standard wallet in the Chia.NET Wallet library.
/// </summary>
public class StandardWallet(
        FullNodeProxy node,
        KeyStore keyStore,
        byte[]? hiddenPuzzleHash = null,
        WalletOptions? walletOptions = null
    ) : Wallet<StandardTransaction>(node, keyStore, walletOptions)
{
    /// <summary>
    /// Gets the hidden puzzle hash.
    /// </summary>
    public byte[] HiddenPuzzleHash { get; } = hiddenPuzzleHash ?? Puzzles.GetPuzzle("defaultHidden").Hash();

    /// <summary>
    /// Sends a fee transaction.
    /// </summary>
    /// <param name="amount">The amount to send as a fee.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of coin spends.</returns>
    public async Task<IEnumerable<CoinSpend>> SendFee(long amount, CancellationToken cancellationToken = default)
    {
        var coinRecords = SelectCoinRecords(amount, CoinSelection.Oldest);

        var spendAmount = BigInteger.Zero;
        foreach (var coinRecord in coinRecords)
        {
            spendAmount += coinRecord.Coin.Amount;
        }

        var change = PuzzleCache[(await FindUnusedIndices(1, [], cancellationToken: cancellationToken))[0]];

        var puzzles = coinRecords.Select(coinRecord => PuzzleCache[CoinRecordIndex(coinRecord)]).ToList();

        var coinSpends = coinRecords.Select((record, i) =>
        {
            var puzzle = puzzles[i];

            var conditions = new List<Program>();

            if (i == 0)
            {
                if (spendAmount > amount)
                {
                    conditions.Add(Program.FromSource($"(51 {HexHelper.FormatHex(change.HashHex())} {spendAmount - amount})"));
                }
            }

            return puzzle.Spend(record.Coin, StandardTransaction.GetSolution(conditions));
        }).ToList();

        return coinSpends;
    }

    /// <summary>
    /// Sends a transaction.
    /// </summary>
    /// <param name="puzzleHash">The puzzle hash.</param>
    /// <param name="amount">The amount to send.</param>
    /// <param name="fee">The fee amount.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of coin spends.</returns>
    public async Task<IEnumerable<CoinSpend>> Send(byte[] puzzleHash, long amount, long fee, CancellationToken cancellationToken = default)
    {
        var totalAmount = amount + fee;

        var coinRecords = SelectCoinRecords(totalAmount, CoinSelection.Oldest);

        var spendAmount = BigInteger.Zero;
        foreach (var coinRecord in coinRecords)
        {
            spendAmount += coinRecord.Coin.Amount;
        }

        var change = PuzzleCache[(await FindUnusedIndices(1, [], cancellationToken: cancellationToken))[0]];

        var puzzles = coinRecords.Select(coinRecord => PuzzleCache[CoinRecordIndex(coinRecord)]).ToList();

        var coinSpends = coinRecords.Select((record, i) =>
        {
            var puzzle = puzzles[i];

            var conditions = new List<Program>();

            if (i == 0)
            {
                conditions.Add(Program.FromSource($"(51 {HexHelper.FormatHex(puzzleHash.ToHex())} {amount})"));

                if (spendAmount > totalAmount)
                {
                    conditions.Add(Program.FromSource($"(51 {HexHelper.FormatHex(change.HashHex())} {spendAmount - totalAmount})"));
                }
            }

            return puzzle.Spend(record.Coin, StandardTransaction.GetSolution(conditions));
        });

        return coinSpends;
    }

    /// <summary>
    /// Creates a puzzle for the specified key pair.
    /// </summary>
    /// <param name="keyPair">The key pair.</param>
    /// <returns>A new instance of the <see cref="StandardTransaction"/> class.</returns>
    public override StandardTransaction CreatePuzzle(KeyPair keyPair) => new(KeyDerivation.CalculateSyntheticPublicKey(keyPair.PublicKey, HiddenPuzzleHash));

    /// <summary>
    /// Signs a spend bundle with the specified aggregated signature me extra data.
    /// </summary>
    /// <param name="spendBundle">The spend bundle to sign.</param>
    /// <param name="aggSigMeExtraData">The aggregated signature me extra data.</param>
    /// <returns>The signed spend bundle.</returns>
    public override SpendBundle SignSpend(SpendBundle spendBundle, byte[] aggSigMeExtraData)
    {
        var syntheticPrivateKeys = KeyStore.Keys
            .Where(keyPair => keyPair.PrivateKey != null)
            .Select(keyPair => KeyDerivation.CalculateSyntheticPrivateKey(keyPair.PrivateKey!, HiddenPuzzleHash))
            .ToList();

        if (KeyStore.PrivateKey != null)
        {
            syntheticPrivateKeys.Add(KeyStore.PrivateKey);
        }

        var privateKeys = KeyStore.Keys
            .Where(keyPair => keyPair.PrivateKey != null)
            .Select(item => item.PrivateKey)
            .ToList();

        syntheticPrivateKeys.AddRange(privateKeys!);

        return Sign.SignSpendBundle(spendBundle, aggSigMeExtraData, true, [.. syntheticPrivateKeys]);
    }
}