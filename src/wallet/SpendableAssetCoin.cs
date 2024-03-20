using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents a spendable asset coin.
/// </summary>
public class SpendableAssetCoin : AssetCoin
{
    /// <summary>
    /// Gets or sets the puzzle program.
    /// </summary>
    public Program Puzzle { get; init; }

    /// <summary>
    /// Gets or sets the inner puzzle program.
    /// </summary>
    public Program InnerPuzzle { get; init; }

    /// <summary>
    /// Gets or sets the inner solution program.
    /// </summary>
    public Program InnerSolution { get; init; }

    /// <summary>
    /// Gets or sets the extra delta value.
    /// </summary>
    public int ExtraDelta { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendableAssetCoin"/> class.
    /// </summary>
    /// <param name="parentCoinSpend">The parent coin spend.</param>
    /// <param name="coin">The coin.</param>
    /// <param name="innerPuzzle">The inner puzzle program.</param>
    /// <param name="innerSolution">The inner solution program.</param>
    /// <param name="extraDelta">The extra delta value.</param>
    /// <param name="assetId">The asset ID.</param>
    public SpendableAssetCoin(CoinSpend parentCoinSpend, Coin coin, Program innerPuzzle, Program innerSolution, int extraDelta = 0, byte[]? assetId = null)
        : base(parentCoinSpend, coin, assetId)
    {
        InnerPuzzle = innerPuzzle;
        InnerSolution = innerSolution;
        ExtraDelta = extraDelta;

        Puzzle = new AssetToken<Program>(AssetId, innerPuzzle);
    }

    /// <summary>
    /// Calculates the subtotals based on the given deltas.
    /// </summary>
    /// <param name="deltas">The deltas.</param>
    /// <returns>An array of subtotals.</returns>
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

    /// <summary>
    /// Calculates the deltas based on the given spendable asset coins.
    /// </summary>
    /// <param name="spendableAssetCoins">The spendable asset coins.</param>
    /// <returns>An array of deltas.</returns>
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