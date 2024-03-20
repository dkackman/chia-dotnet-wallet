using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

public class AssetToken<T>(byte[] assetId, T innerPuzzle) : Program(Puzzles.GetPuzzle("cat").Curry([
            FromBytes(Puzzles.GetPuzzle("cat").Hash()),
            FromBytes(assetId),
            innerPuzzle
        ]).Value) where T : Program
{
    
    public byte[] AssetId { get; init; } = assetId;
    public T InnerPuzzle { get; init; } = innerPuzzle;

    public static Program CalculateIssuePayment(Program tail, Program solution, byte[] innerPuzzleHash, int amount)
    {
        return FromCons(
            FromInt(1),
            FromList([
                FromList([
                    FromInt(51),
                    FromInt(0),
                    FromInt(-113),
                    tail,
                    solution
                ]),
                FromList([
                    FromInt(51),
                    FromBytes(innerPuzzleHash),
                    FromInt(amount),
                    FromList([FromBytes(innerPuzzleHash)])
                ])
            ])
        );
    }

    public static AssetToken<Program> CalculatePuzzle(Program tail, Program solution, byte[] innerPuzzleHash, int amount)
    {
        return new AssetToken<Program>(innerPuzzleHash,
            Puzzles.GetPuzzle("cat").Curry([
                FromBytes(Puzzles.GetPuzzle("cat").Hash()),
                FromBytes(tail.Hash()),
                CalculateIssuePayment(tail, solution, innerPuzzleHash, amount)
            ]));
    }

    public static CoinSpend Issue(CoinSpend originCoinSpend, Program tail, Program solution, byte[] innerPuzzleHash, int amount)
    {
        var payToPuzzle = CalculateIssuePayment(tail, solution, innerPuzzleHash, amount);
        var catPuzzle = CalculatePuzzle(tail, solution, innerPuzzleHash, amount);

        var eveCoin = new Coin
        {
            ParentCoinInfo = HexHelper.FormatHex(originCoinSpend.Coin.CoinId.ToHex()),
            PuzzleHash = HexHelper.FormatHex(catPuzzle.HashHex()),
            Amount = amount
        };

        var spendableEve = new SpendableAssetCoin(originCoinSpend, eveCoin, payToPuzzle, Nil, 0, tail.Hash());

        return Spend([spendableEve])[0];
    }

    /// <summary>
    /// Spends a list of spendable asset coins by creating coin spends.
    /// </summary>
    /// <param name="spendableAssetCoins">The list of spendable asset coins to spend.</param>
    /// <returns>A list of coin spends.</returns>
    public static List<CoinSpend> Spend(List<SpendableAssetCoin> spendableAssetCoins)
    {
        if (spendableAssetCoins.Count == 0)
            throw new Exception("Missing spendable asset coin.");

        var assetId = spendableAssetCoins[0].AssetId;

        foreach (var item in spendableAssetCoins.Skip(1))
        {
            if (!ByteUtils.BytesEqual(item.AssetId, assetId))
            {
                throw new Exception("Mixed asset ids in spend.");
            }
        }

        var deltas = SpendableAssetCoin.CalculateDeltas(spendableAssetCoins);
        var subtotals = SpendableAssetCoin.CalculateSubtotals(deltas);

        return spendableAssetCoins.Select((spendableAssetCoin, i) =>
        {
            var previous = (i - 1) % spendableAssetCoins.Count;
            var next = (i + 1) % spendableAssetCoins.Count;

            var previousCoin = spendableAssetCoins[previous];
            var nextCoin = spendableAssetCoins[next];

            var solution = FromList([
                spendableAssetCoin.InnerSolution,
                spendableAssetCoin.LineageProof,
                FromBytes(previousCoin.Coin.CoinId),
                FromList([
                    FromHex(HexHelper.SanitizeHex(spendableAssetCoin.Coin.ParentCoinInfo)), 
                    FromHex(HexHelper.SanitizeHex(spendableAssetCoin.Coin.PuzzleHash)),
                    FromBigInt(spendableAssetCoin.Coin.Amount),
                ]),
                FromList([
                    FromHex(HexHelper.SanitizeHex(nextCoin.Coin.ParentCoinInfo)),
                    FromBytes(nextCoin.InnerPuzzle.Hash()),
                    FromBigInt(nextCoin.Coin.Amount),
                ]),
                Program.FromInt(subtotals[i]),
                FromInt(spendableAssetCoin.ExtraDelta),
            ]);

            return new CoinSpend
            {
                Coin = spendableAssetCoin.Coin,
                PuzzleReveal = spendableAssetCoin.Puzzle.SerializeHex(),
                Solution = solution.SerializeHex(),
            };
        }).ToList();
    }
}