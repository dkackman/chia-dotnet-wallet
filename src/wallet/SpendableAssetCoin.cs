using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

public class SpendableAssetCoin : AssetCoin
{
    public Program Puzzle { get; init; }
    public Program InnerPuzzle { get; init; }
    public Program InnerSolution { get; init; }
    public int ExtraDelta { get; init; }

    public SpendableAssetCoin(CoinSpend parentCoinSpend, Coin coin, Program innerPuzzle, Program innerSolution, int extraDelta = 0, byte[]? assetId = null)
        : base(parentCoinSpend, coin, assetId)
    {
        InnerPuzzle = innerPuzzle;
        InnerSolution = innerSolution;
        ExtraDelta = extraDelta;

        Puzzle = new AssetToken<Program>(AssetId, innerPuzzle);
    }

    public static long[] CalculateSubtotals(long[] deltas)
    {
        long subtotal = 0;

        var subtotals = deltas.Select(delta =>
        {
            var current = subtotal;

            subtotal += delta;

            return current;
        });

        var offset = subtotals.Min();

        return subtotals.Select(value => value - offset).ToArray();
    }

    public static long[] CalculateDeltas(List<SpendableAssetCoin> spendableAssetCoins)
    {
        return spendableAssetCoins.Select(spendableAssetCoin =>
        {
            var conditions = spendableAssetCoin.InnerPuzzle.Run(spendableAssetCoin.InnerSolution).Value.ToList();

            long total = -spendableAssetCoin.ExtraDelta;

            foreach (var condition in conditions)
            {
                if (condition.IsAtom)
                {
                    continue;
                }

                var items = condition.ToList();
                if (items.Count < 3)
                {
                    continue;
                }

                if (
                    items[0].IsCons ||
                    items[1].IsCons ||
                    items[2].IsCons ||
                    items[0].ToBigInt() != 51 ||
                    items[2].ToBigInt() == -113
                )
                {
                    continue;
                }

                total += items[2].ToInt();
            }

            return total;
        }).ToArray();
    }
}