using System.Numerics;

namespace chia.dotnet.wallet;

/// <summary>
/// Interface for a wallet that can sign and submit transactions.
/// </summary>
public interface IWallet
{
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
}
