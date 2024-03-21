using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents a collection of static methods for creating puzzle programs.
/// </summary>
public static class Tails
{
    /// <summary>
    /// Creates a puzzle program using the "delegated" puzzle with the specified public key.
    /// </summary>
    /// <param name="publicKey">The public key to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program Delegated(JacobianPoint publicKey) =>
        Puzzles.GetPuzzle("delegated").Curry([Program.FromJacobianPoint(publicKey)]);

    /// <summary>
    /// Creates a puzzle program using the "everythingWithSignature" puzzle with the specified public key.
    /// </summary>
    /// <param name="publicKey">The public key to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program EverythingWithSignature(JacobianPoint publicKey) =>
        Puzzles.GetPuzzle("everythingWithSignature").Curry([Program.FromJacobianPoint(publicKey)]);

    /// <summary>
    /// Creates a puzzle program using the "genesisByCoinId" puzzle with the specified coin ID.
    /// </summary>
    /// <param name="coinId">The coin ID to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program GenesisByCoinId(byte[] coinId) =>
        Puzzles.GetPuzzle("genesisByCoinId").Curry([Program.FromBytes(coinId)]);

    /// <summary>
    /// Creates a puzzle program using the "indexedWithSignature" puzzle with the specified public key and index.
    /// </summary>
    /// <param name="publicKey">The public key to be used in the puzzle program.</param>
    /// <param name="index">The index to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program IndexedWithSignature(JacobianPoint publicKey, int index) =>
        Puzzles.GetPuzzle("indexedWithSignature").Curry([Program.FromJacobianPoint(publicKey), Program.FromInt(index)]);

    /// <summary>
    /// Creates a puzzle program using the "meltableGenesisByCoinId" puzzle with the specified coin ID.
    /// </summary>
    /// <param name="coinId">The coin ID to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program MeltableGenesisByCoinId(byte[] coinId) =>
        Puzzles.GetPuzzle("meltableGenesisByCoinId").Curry([Program.FromBytes(coinId)]);
}