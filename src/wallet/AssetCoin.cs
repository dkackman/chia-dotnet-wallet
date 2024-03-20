using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

public class AssetCoin
{
    public CoinSpend ParentCoinSpend { get; init; }
    public byte[] AssetId { get; init; }
    public Program LineageProof { get; init; }
    public Coin Coin { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetCoin"/> class.
    /// </summary>
    /// <param name="parentCoinSpend">The parent coin spend.</param>
    /// <param name="coin">The coin.</param>
    /// <param name="assetId">The asset ID.</param>
    public AssetCoin(CoinSpend parentCoinSpend, Coin coin, byte[]? assetId = null)
    {
        ParentCoinSpend = parentCoinSpend;
        Coin = coin;

        if (assetId != null)
        {
            AssetId = assetId;
            LineageProof = Program.Nil;
        }
        else
        {
            var parentPuzzleReveal = Program.DeserializeHex(HexHelper.SanitizeHex(parentCoinSpend.PuzzleReveal));

            var parentPuzzleUncurried = parentPuzzleReveal.Uncurry() ?? throw new Exception("Could not uncurry parent puzzle reveal.");
            var parentPuzzle = parentPuzzleUncurried.Item1;
            var parentArguments = parentPuzzleUncurried.Item2.ToList();

            if (!parentPuzzle.Equals(Puzzles.GetPuzzle("cat")))
                throw new Exception("Parent puzzle is not asset token.");

            if (parentArguments.Count <= 2)
                throw new Exception("Invalid parent puzzle reveal.");

            AssetId = parentArguments[1].Atom;
            LineageProof = Program.FromList([
                Program.FromHex(HexHelper.SanitizeHex(parentCoinSpend.Coin.ParentCoinInfo)),
                Program.FromBytes(parentArguments[2].Hash()),
                Program.FromBigInt(parentCoinSpend.Coin.Amount)
            ]);
        }
    }
}