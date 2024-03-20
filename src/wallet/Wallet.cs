using chia.dotnet.bls;
using chia.dotnet.clvm;
using System.Numerics;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents an abstract wallet class that provides common functionality for different types of wallets.
/// </summary>
/// <typeparam name="T">The type of program associated with the wallet.</typeparam>
public abstract class Wallet<T>(FullNodeProxy node, KeyStore keyStore, WalletOptions? walletOptions = null) where T : Program
{
    /// <summary>
    /// Gets or sets the <see cref="FullNodeProxy"/> instance used for interacting with the Chia full node.
    /// </summary>
    public FullNodeProxy Node { get; init; } = node;

    /// <summary>
    /// Gets or sets the <see cref="KeyStore"/> instance used for managing keys.
    /// </summary>
    public KeyStore KeyStore { get; init; } = keyStore;

    /// <summary>
    /// Gets or sets the <see cref="WalletOptions"/> instance used for configuring wallet options.
    /// </summary>
    public WalletOptions Options { get; init; } = walletOptions ?? new WalletOptions();

    /// <summary>
    /// Gets or sets the list of lists of <see cref="CoinRecord"/> instances representing the coin records associated with the wallet.
    /// </summary>
    public List<List<CoinRecord>> CoinRecords { get; private set; } = [];

    /// <summary>
    /// Gets the list of <see cref="CoinRecord"/> instances representing the artificial coin records associated with the wallet.
    /// </summary>
    public List<CoinRecord> ArtificialCoinRecords { get; } = [];

    /// <summary>
    /// Gets the list of <typeparamref name="T"/> instances representing the puzzle cache associated with the wallet.
    /// </summary>
    public List<T> PuzzleCache { get; } = [];

    /// <summary>
    /// Signs the given spend bundle with the wallet's private key and returns the signed spend bundle.
    /// </summary>
    /// <param name="spendBundle">The spend bundle to sign.</param>
    /// <param name="aggSigMeExtraData">The extra data to include in the aggregated signature.</param>
    /// <returns>The signed spend bundle.</returns>
    public abstract SpendBundle SignSpend(SpendBundle spendBundle, byte[] aggSigMeExtraData);

    /// <summary>
    /// Creates a new instance of the program associated with the wallet using the given key pair.
    /// </summary>
    /// <param name="keyPair">The key pair to use for creating the program.</param>
    /// <returns>The created program instance.</returns>
    public abstract T CreatePuzzle(KeyPair keyPair);

    /// <summary>
    /// Gets the index of the given coin record in the puzzle cache.
    /// </summary>
    /// <param name="coinRecord">The coin record to find the index of.</param>
    /// <returns>The index of the coin record in the puzzle cache.</returns>
    public int CoinRecordIndex(CoinRecord coinRecord) => PuzzleCache.FindIndex(puzzle => coinRecord.Coin.PuzzleHash == puzzle.HashHex().FormatHex());

    /// <summary>
    /// Synchronizes the wallet with the Chia network, fetching new coin records and updating the puzzle cache.
    /// </summary>
    /// <param name="overrideOptions">The wallet options to use for synchronization. If null, the default wallet options will be used.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous synchronization operation.</returns>
    public async Task Sync(WalletOptions? overrideOptions = null, CancellationToken cancellationToken = default)
    {
        var options = overrideOptions ?? Options;

        int keyCount = KeyStore.Keys.Count;
        int unusedCount = 0;

        for (int i = CoinRecords.Count - 1; i >= 0; i--)
        {
            var coinRecords = CoinRecords[i];

            if (coinRecords.Count == 0)
            {
                unusedCount++;
            }
            else
            {
                break;
            }
        }

        while (keyCount < options.MaxAddressCount &&
               (unusedCount < options.UnusedAddressCount || keyCount < options.MinAddressCount))
        {
            KeyStore.Generate(1);
            var keyPair = KeyStore.Keys[^1];
            var puzzle = CreatePuzzle(keyPair);
            var coinRecords = await Node.GetCoinRecordsByPuzzleHash(puzzle.HashHex(), true, null, null, cancellationToken);

            keyCount++;
            if (!coinRecords.Any())
            {
                unusedCount++;
            }
            else
            {
                unusedCount = 0;
            }
        }

        for (int i = PuzzleCache.Count; i < KeyStore.Keys.Count; i++)
        {
            PuzzleCache.Add(CreatePuzzle(KeyStore.Keys[i]));
        }

        await FetchCoinRecords(cancellationToken);
    }

    /// <summary>
    /// Fetches the coin records associated with the wallet from the Chia network.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>An awaitable Task</returns>
    public async Task FetchCoinRecords(CancellationToken cancellationToken = default)
    {
        var puzzleHashes = PuzzleCache.Select(puzzle => puzzle.HashHex());

        var coinRecordResult = await Node.GetCoinRecordsByPuzzleHashes(puzzleHashes, true, null, null, cancellationToken);

        foreach (var artificialCoinRecord in ArtificialCoinRecords)
        {
            if (coinRecordResult.Any(coinRecord => ByteUtils.BytesEqual(coinRecord.Coin.CoinId, artificialCoinRecord.Coin.CoinId)))
            {
                ArtificialCoinRecords.Remove(artificialCoinRecord);
            }
        }

        var coinRecords = coinRecordResult.Concat(ArtificialCoinRecords);

        var newCoinRecords = new List<List<CoinRecord>>();

        foreach (var puzzleHash in puzzleHashes)
        {
            newCoinRecords.Add(coinRecords.Where(coinRecord => HexHelper.SanitizeHex(coinRecord.Coin.PuzzleHash) == puzzleHash).ToList());
        }

        CoinRecords = newCoinRecords;
    }

    /// <summary>
    /// Clears the unconfirmed transactions associated with the wallet.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ClearUnconfirmedTransactions(CancellationToken cancellationToken = default)
    {
        ArtificialCoinRecords.Clear();
        await FetchCoinRecords(cancellationToken);
    }

    /// <summary>
    /// Creates a new spend bundle.
    /// </summary>
    /// <returns>SpendBundle</returns>
    public SpendBundle CreateSpend() => new()
        {
            CoinSpends = [],
            AggregatedSignature = JacobianPoint.InfinityG2().ToHex()
        };

    /// <summary>
    /// Finds the unused indices for the wallet.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="used"></param>
    /// <param name="presynced"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The list of indices</returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<int>> FindUnusedIndices(int amount, List<int> used, bool presynced = false, CancellationToken cancellationToken = default)
    {
        var result = new List<int>();

        for (int i = 0; i <= CoinRecords.Count; i++)
        {
            var coinRecords = CoinRecords[i];

            if (coinRecords.Count == 0 && !used.Contains(i))
            {
                result.Add(i);
            }

            if (result.Count == amount)
            {
                break;
            }
        }

        if (result.Count < amount)
        {
            if (!presynced)
            {
                amount += used.Count;

                await Sync(amount > Options.UnusedAddressCount ? new WalletOptions { UnusedAddressCount = amount } : null, cancellationToken);

                return await FindUnusedIndices(amount, used, true, cancellationToken);
            }
            else
            {
                throw new Exception("Could not find enough unused indices.");
            }
        }

        foreach (var index in result)
        {
            used.Add(index);
        }

        return result;
    }

    /// <summary>
    /// Gets the balance of the wallet.
    /// </summary>
    /// <returns>The balance</returns>
    public BigInteger GetBalance()
    {
        var result = BigInteger.Zero;
        foreach (var record in CoinRecords
                                .SelectMany(x => x)
                                .Where(coinRecord => !coinRecord.Spent && coinRecord.SpentBlockIndex <= 0))
        {
            result += record.Coin.Amount;
        }

        return result;
    }

    /// <summary>
    /// Selects coin records for spending.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="coinSelection"></param>
    /// <param name="minimumCoinRecords"></param>
    /// <param name="required"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public List<CoinRecord> SelectCoinRecords(BigInteger amount, CoinSelection coinSelection, int minimumCoinRecords = 0, bool required = true)
    {
        var coinRecords = CoinRecords.SelectMany(x => x).ToList();

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
        var result = await Node.PushTx(spendBundle, cancellationToken);

        if (!result) throw new Exception("Could not push transaction.");

        if (!Options.InstantCoinRecords)
        {
            return;
        }

        var newCoinRecords = new List<CoinRecord>();

        foreach (var coinSpend in spendBundle.CoinSpends)
        {
            var output = Program.DeserializeHex(coinSpend.PuzzleReveal).Run(Program.DeserializeHex(coinSpend.Solution)).Value;

            if (output.IsCons)
            {
                continue;
            }
            var conditions = output.ToList();

            foreach (var condition in conditions)
            {
                if (condition.IsAtom)
                {
                    continue;
                }

                var conditionData = condition.ToList();

                if (conditionData.Count < 3 || conditionData[0].IsCons || conditionData[1].IsCons || conditionData[2].IsCons)
                {
                    continue;
                }
                if (conditionData[0].ToBigInt() != 51)
                {
                    continue;
                }
                var puzzleHash = conditionData[1].ToHex();
                var amount = conditionData[2].ToInt();

                var coin = new Coin
                {
                    ParentCoinInfo = HexHelper.FormatHex(coinSpend.Coin.CoinId.ToHex()),
                    PuzzleHash = HexHelper.FormatHex(puzzleHash),
                    Amount = amount
                };

                bool spent = spendBundle.CoinSpends.Any(check => ByteUtils.BytesEqual(check.Coin.CoinId, coin.CoinId));

                var coinRecord = new CoinRecord
                {
                    Coin = coin,
                    ConfirmedBlockIndex = 0,
                    SpentBlockIndex = spent ? 1 : (uint)0,
                    Spent = spent,
                    Coinbase = false,
                    Timestamp = DateTime.Now.ToTimestamp()
                };

                newCoinRecords.Add(coinRecord);
            }
        }

        foreach (var coinRecord in newCoinRecords)
        {
            ArtificialCoinRecords.Add(coinRecord);
        }

        await FetchCoinRecords(cancellationToken);
    }
}
