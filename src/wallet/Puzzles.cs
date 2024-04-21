using chia.dotnet.bls;
using chia.dotnet.clvm;
using System.Reflection;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents a collection of methods for creating puzzle programs.
/// </summary>
public static class Puzzles
{
    /// <summary>
    /// Dictionary that stores the puzzle programs.
    /// </summary>
    private static readonly Dictionary<string, Program> puzzles = new()
    {
        { "cat", LoadPuzzle("cat") },
        { "syntheticPublicKey", LoadPuzzle("synthetic_public_key") },
        { "defaultHidden", LoadPuzzle("default_hidden") },
        { "payToConditions", LoadPuzzle("pay_to_conditions") },
        { "payToDelegatedOrHidden", LoadPuzzle("pay_to_delegated_or_hidden") },
        { "delegated", LoadPuzzle("delegated", "tails") },
        { "everythingWithSignature", LoadPuzzle("everything_with_signature", "tails") },
        { "indexedWithSignature", LoadPuzzle("indexed_with_signature", "tails") },
        { "genesisByCoinId", LoadPuzzle("genesis_by_coin_id", "tails") },
        { "meltableGenesisByCoinId", LoadPuzzle("meltable_genesis_by_coin_id", "tails") },
    };

    /// <summary>
    /// Gets the Cat puzzle program.
    /// </summary>
    public static Program Cat => GetPuzzle("cat");

    /// <summary>
    /// Gets the syntheticPublicKey puzzle program.
    /// </summary>
    public static Program SyntheticPublicKey => GetPuzzle("syntheticPublicKey");

    /// <summary>
    /// Gets the payToConditions puzzle program.
    /// </summary>
    public static Program PayToConditions => GetPuzzle("payToConditions");

    /// <summary>
    /// Gets the payToDelegatedOrHidden puzzle program.
    /// </summary>
    public static Program PayToDelegatedOrHidden => GetPuzzle("payToDelegatedOrHidden");

    /// <summary>
    /// Creates a puzzle program using the "delegated" puzzle with the specified public key.
    /// </summary>
    /// <param name="publicKey">The public key to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program Delegated(JacobianPoint publicKey) => GetPuzzle("delegated").Curry([Program.FromJacobianPoint(publicKey)]);

    /// <summary>
    /// Creates a puzzle program using the "everythingWithSignature" puzzle with the specified public key.
    /// </summary>
    /// <param name="publicKey">The public key to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program EverythingWithSignature(JacobianPoint publicKey) => GetPuzzle("everythingWithSignature").Curry([Program.FromJacobianPoint(publicKey)]);

    /// <summary>
    /// Creates a puzzle program using the "genesisByCoinId" puzzle with the specified coin ID.
    /// </summary>
    /// <param name="coinId">The coin ID to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program GenesisByCoinId(byte[] coinId) => GetPuzzle("genesisByCoinId").Curry([Program.FromBytes(coinId)]);

    /// <summary>
    /// Creates a puzzle program using the "indexedWithSignature" puzzle with the specified public key and index.
    /// </summary>
    /// <param name="publicKey">The public key to be used in the puzzle program.</param>
    /// <param name="index">The index to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program IndexedWithSignature(JacobianPoint publicKey, int index) => GetPuzzle("indexedWithSignature").Curry([Program.FromJacobianPoint(publicKey), Program.FromInt(index)]);

    /// <summary>
    /// Creates a puzzle program using the "meltableGenesisByCoinId" puzzle with the specified coin ID.
    /// </summary>
    /// <param name="coinId">The coin ID to be used in the puzzle program.</param>
    /// <returns>A puzzle program.</returns>
    public static Program MeltableGenesisByCoinId(byte[] coinId) => GetPuzzle("meltableGenesisByCoinId").Curry([Program.FromBytes(coinId)]);

    /// <summary>
    /// Gets the puzzle program with the specified name.
    /// </summary>
    /// <param name="name">The name of the puzzle program.</param>
    /// <returns>The puzzle program.</returns>
    internal static Program GetPuzzle(string name) => puzzles[name];

    /// <summary>
    /// Loads a puzzle program from a resource file.
    /// </summary>
    /// <param name="name">The name of the puzzle program.</param>
    /// <param name="folder">The folder where the resource file is located.</param>
    /// <returns>The loaded puzzle program.</returns>
    private static Program LoadPuzzle(string name, string? folder = null)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = "wallet.puzzles";
        if (!string.IsNullOrEmpty(folder))
        {
            resourcePath = Path.Combine(resourcePath, folder);
        }

        resourcePath = Path.Combine(resourcePath, $"{name}.clvm.hex");
        resourcePath = resourcePath.Replace(Path.DirectorySeparatorChar, '.');

        using Stream stream = assembly.GetManifestResourceStream(resourcePath) ?? throw new InvalidOperationException($"Could not find resource: {resourcePath}");
        using StreamReader reader = new(stream);
        var fileContent = reader.ReadToEnd().Trim();

        return Program.DeserializeHex(fileContent);
    }
}