using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

public class StandardTransaction(JacobianPoint syntheticPublicKey) : Program(Puzzles.GetPuzzle("payToDelegatedOrHidden").Curry([FromJacobianPoint(syntheticPublicKey)]).Value)
{
    public JacobianPoint SyntheticPublicKey { get; init; } = syntheticPublicKey;

    public static Program GetSolution(List<Program> conditions)
    {
        var delegatedPuzzle = Puzzles.GetPuzzle("payToConditions").Run(FromList([FromList(conditions)])).Value;

        return FromList([Nil, delegatedPuzzle, Nil]);
    }

    public CoinSpend Spend(Coin coin, Program solution) => new()
    {
        Coin = coin,
        PuzzleReveal = SerializeHex(),
        Solution = solution.SerializeHex()
    };
}
