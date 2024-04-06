using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents an asset wallet that manages asset tokens.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AssetWallet"/> class.
/// </remarks>
/// <param name="node">The full node proxy.</param>
/// <param name="keyStore">The key store.</param>
/// <param name="assetId">The asset ID.</param>
/// <param name="hiddenPuzzleHash">The hidden puzzle hash.</param>
/// <param name="walletOptions">The wallet options.</param>
public class AssetWallet(
        FullNodeProxy node,
        KeyStore keyStore,
        byte[] assetId,
        byte[]? hiddenPuzzleHash = null,
        WalletOptions? walletOptions = null
    ) : Wallet<AssetToken<StandardTransaction>>(node, keyStore, walletOptions)
{

    /// <summary>
    /// Gets the asset ID.
    /// </summary>
    public byte[] AssetId { get; init; } = assetId;

    /// <summary>
    /// Gets the hidden puzzle hash.
    /// </summary>
    public override byte[] HiddenPuzzleHash { get; init; } = hiddenPuzzleHash ?? Puzzles.GetPuzzle("defaultHidden").Hash();

    private Program? Tail { get; set; } = null;

    /// <summary>
    /// Creates a puzzle for the specified key pair.
    /// </summary>
    /// <param name="keyPair">The key pair.</param>
    /// <returns>The created asset token puzzle.</returns>
    public override AssetToken<StandardTransaction> CreatePuzzle(KeyPair keyPair) =>
        new(
            AssetId,
            new StandardTransaction(
                KeyDerivation.CalculateSyntheticPublicKey(
                    keyPair.PublicKey,
                    HiddenPuzzleHash
                )
            )
        );

    /// <summary>
    /// Gets the parent coin spend for the specified coin record.
    /// </summary>
    /// <param name="coinRecord">The coin record.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parent coin spend.</returns>
    public async Task<CoinSpend> GetParentCoinSpend(CoinRecord coinRecord, CancellationToken cancellationToken = default)
    {
        var parentResult = await Node.GetCoinRecordByName(coinRecord.Coin.ParentCoinInfo, cancellationToken);

        var parentCoinSpendResult = await Node.GetPuzzleAndSolution(
            coinRecord.Coin.ParentCoinInfo,
            parentResult.SpentBlockIndex,
            cancellationToken
        );

        return parentCoinSpendResult;
    }

    /// <summary>
    /// Finds the tail of the asset token.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tail of the asset token.</returns>
    public async Task<Program?> FindTail(CancellationToken cancellationToken = default)
    {
        if (Tail is not null)
        {
            return Tail;
        }

        var coinRecord = CoinRecords.SelectMany(x => x).FirstOrDefault();

        if (coinRecord is null)
        {
            return null;
        }

        while (true)
        {
            var eveCoinRecord = coinRecord;

            coinRecord = await Node.GetCoinRecordByName(coinRecord.Coin.ParentCoinInfo, cancellationToken);

            var coinSpend = await Node.GetPuzzleAndSolution(coinRecord.Coin.CoinId.ToHex(), coinRecord.SpentBlockIndex, cancellationToken);

            var puzzle = Program.DeserializeHex(coinSpend.PuzzleReveal.Remove0x());

            var uncurriedPuzzle = puzzle.Uncurry();
            if (uncurriedPuzzle is null)
            {
                continue;
            }

            var puzzleMod = uncurriedPuzzle.Item1;
            if (puzzleMod.Equals(Puzzles.GetPuzzle("cat")))
            {
                continue;
            }
            if (eveCoinRecord.SpentBlockIndex == 0)
            {
                continue;
            }

            var eveCoinSpend = await Node.GetPuzzleAndSolution(eveCoinRecord.Coin.CoinId.ToHex(), eveCoinRecord.SpentBlockIndex, cancellationToken);
            var evePuzzle = Program.DeserializeHex(eveCoinSpend.PuzzleReveal.Remove0x());
            var uncurriedEvePuzzle = evePuzzle.Uncurry();

            if (uncurriedEvePuzzle == null || !uncurriedEvePuzzle.Item1.Equals(Puzzles.GetPuzzle("cat")))
                throw new Exception("Eve is not an asset token.");

            var result = uncurriedEvePuzzle.Item2.ToList()[2].Run(Program.Nil).Value;

            if (result.IsAtom)
                throw new Exception("Asset spend output is atom.");

            var conditions = result.ToList();

            foreach (var condition in conditions)
            {
                if (condition.IsAtom)
                {
                    continue;
                }

                var args = condition.ToList();
                if (args.Count < 5)
                {
                    continue;
                }

                if (args[0].IsCons || args[0].ToBigInt() != 51)
                {
                    continue;
                }

                if (args[2].IsCons || args[2].ToBigInt() != -113)
                {
                    continue;
                }

                Tail = args[3];

                return Tail;
            }

            break;
        }

        throw new Exception("Coin record is not a genesis.");
    }

    /// <summary>
    /// Signs the spend bundle with the private keys in the key store.
    /// </summary>
    /// <param name="spendBundle">The spend bundle to sign.</param>
    /// <param name="aggSigMeExtraData">The aggregated signature me extra data.</param>
    /// <returns>The signed spend bundle.</returns>
    public override SpendBundle SignSpend(SpendBundle spendBundle, byte[] aggSigMeExtraData)
    {
        var keysWithPrivate = KeyStore.Keys.Where(keyPair => keyPair.PrivateKey != null);

        var syntheticPrivateKeys = keysWithPrivate.Select(keyPair => KeyDerivation.CalculateSyntheticPrivateKey(keyPair.PrivateKey!.Value, HiddenPuzzleHash)).ToList();

        if (KeyStore.PrivateKey is not null)
        {
            syntheticPrivateKeys.Add(KeyStore.PrivateKey.Value);
        }

        var privateKeys = keysWithPrivate.Select(item => item.PrivateKey!.Value);
        syntheticPrivateKeys.AddRange(privateKeys!);

        return Sign.SignSpendBundle(spendBundle, aggSigMeExtraData, true, [.. syntheticPrivateKeys]);
    }
}
