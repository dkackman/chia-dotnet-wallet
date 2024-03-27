using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet
{
    /// <summary>
    /// Represents a standard transaction in the Chia.NET wallet.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="StandardTransaction"/> class with the specified synthetic public key.
    /// </remarks>
    /// <param name="syntheticPublicKey">The synthetic public key to use.</param>
    public class StandardTransaction(JacobianPoint syntheticPublicKey)
        : Program(Puzzles.GetPuzzle("payToDelegatedOrHidden").Curry([FromJacobianPoint(syntheticPublicKey)]).Value)
    {

        /// <summary>
        /// Gets or sets the synthetic public key associated with the transaction.
        /// </summary>
        public JacobianPoint SyntheticPublicKey { get; init; } = syntheticPublicKey;

        /// <summary>
        /// Gets the solution program for the specified conditions.
        /// </summary>
        /// <param name="conditions">The conditions to use in the solution.</param>
        /// <returns>The solution program.</returns>
        public static Program GetSolution(List<Program> conditions)
        {
            var delegatedPuzzle = Puzzles.GetPuzzle("payToConditions").Run(FromList([FromList(conditions)])).Value;

            return FromList([Nil, delegatedPuzzle, Nil]);
        }

        /// <summary>
        /// Creates a coin spend using the specified coin and solution program.
        /// </summary>
        /// <param name="coin">The coin to spend.</param>
        /// <param name="solution">The solution program to use.</param>
        /// <returns>The created coin spend.</returns>
        public CoinSpend Spend(Coin coin, Program solution) => new()
        {
            Coin = coin,
            PuzzleReveal = SerializeHex(),
            Solution = solution.SerializeHex()
        };
    }
}
