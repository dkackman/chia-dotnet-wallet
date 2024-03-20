using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

public class AssetWallet(
        FullNodeProxy node,
        KeyStore keyStore,
        byte[] assetId,
        byte[]? hiddenPuzzleHash = null,
        WalletOptions? walletOptions = null
    ) : Wallet<AssetToken<StandardTransaction>>(node, keyStore, walletOptions)
{
    public byte[] AssetId { get; init; } = assetId;
    public byte[] HiddenPuzzleHash { get; init; } = hiddenPuzzleHash ?? Puzzles.GetPuzzle("defaultHidden").Hash();
    private Program? Tail { get; set; } = null;

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

            var puzzle = Program.DeserializeHex(HexHelper.SanitizeHex(coinSpend.PuzzleReveal));

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

            var evePuzzle = Program.DeserializeHex(HexHelper.SanitizeHex(eveCoinSpend.PuzzleReveal));

            var uncurriedEvePuzzle = evePuzzle.Uncurry();
            if (uncurriedEvePuzzle == null || !uncurriedEvePuzzle.Item1.Equals(Puzzles.GetPuzzle("cat")))
                throw new Exception("Eve is not an asset token.");

            var result = uncurriedEvePuzzle.Item2.ToList()[2].Run(Program.Nil).Value;

            if (result.IsAtom) throw new Exception("Asset spend output is atom.");

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

    public override SpendBundle SignSpend(SpendBundle spendBundle, byte[] aggSigMeExtraData)
    {
        var keysWithPrivate = KeyStore.Keys.Where(keyPair => keyPair.PrivateKey != null);
        var syntheticPrivateKeys = keysWithPrivate.Select(keyPair => KeyDerivation.CalculateSyntheticPrivateKey(keyPair.PrivateKey!, HiddenPuzzleHash)).ToList();

        if (KeyStore.PrivateKey is not null)
        {
            syntheticPrivateKeys.Add(KeyStore.PrivateKey);
        }

        var privateKeys = keysWithPrivate.Select(item => item.PrivateKey);
        syntheticPrivateKeys.AddRange(privateKeys!);

        return Sign.SignSpendBundle(spendBundle, aggSigMeExtraData, true, [.. syntheticPrivateKeys]);
    }
}