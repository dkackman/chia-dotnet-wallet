// using chia.dotnet;
// using chia.dotnet.bls;
// using chia.dotnet.clvm;

// namespace chia.dotnet.wallet;

// public class StandardTransaction : Program
// {
//     public JacobianPoint SyntheticPublicKey { get; }

//     public StandardTransaction(JacobianPoint syntheticPublicKey)
//         : base(Puzzles.PayToDelegatedOrHidden.Curry(new List<Program> { Program.FromJacobianPoint(syntheticPublicKey) }).Value)
//     {
//         SyntheticPublicKey = syntheticPublicKey;
//     }

//     public Program GetSolution(List<Program> conditions)
//     {
//         var delegatedPuzzle = Puzzles.PayToConditions.Run(Program.FromList(new List<Program> { Program.FromList(conditions) })).Value;

//         return Program.FromList(new List<Program> { Program.Nil, delegatedPuzzle, Program.Nil });
//     }

//     public CoinSpend Spend(Coin coin, Program solution) => new()
//     {
//         Coin = coin,
//         PuzzleReveal = SerializeHex(),
//         Solution = solution.SerializeHex()
//     };
// }
