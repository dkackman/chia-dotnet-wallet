using System.Numerics;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

/// <summary>
/// Interface for a wallet that can sign and submit transactions.
/// </summary>
public interface IWallet
{
    /// <summary>
    /// Waits for the wallet to sync.
    /// </summary>
    /// <returns></returns>
    Task WaitForSync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The hidden puzzle hash
    /// </summary>
    byte[] HiddenPuzzleHash { get; init; }

    /// <summary>
    /// Submits a spend bundle to the blockchain.
    /// </summary>
    /// <param name="spendBundle"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CompleteSpend(SpendBundle spendBundle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the confirmed balance of the wallet.
    /// </summary>
    /// <returns></returns>
    Task<BigInteger> GetBalance();

    /// <summary>
    /// Selects coin records to spend.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="coinSelection"></param>
    /// <param name="minimumCoinRecords"></param>
    /// <param name="required"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<CoinRecord>> SelectCoinRecords(BigInteger amount, CoinSelection coinSelection, int minimumCoinRecords = 0, bool required = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs a spend bundle.
    /// </summary>
    /// <param name="spendBundle"></param>
    /// <param name="aggSigMeExtraData"></param>
    /// <returns></returns>
    SpendBundle SignSpend(SpendBundle spendBundle, byte[] aggSigMeExtraData);

    /// <summary>
    /// Finds a puzzle by hash.
    /// </summary>
    /// <param name="puzzleHash"></param>
    /// <returns></returns>
    Program FindProgram(string puzzleHash);

    /// <summary>
    /// Indicates whether the provided program is ours.
    /// </summary>
    /// <param name="revealProgram"></param>
    /// <returns></returns>
    bool IsOurs(Program revealProgram);
}
