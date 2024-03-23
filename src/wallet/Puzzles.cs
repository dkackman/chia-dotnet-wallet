using System.Reflection;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

internal static class Puzzles
{
    private static readonly Dictionary<string, Program> puzzles = new()
    {
        { "cat", Puzzle("cat") },
        { "syntheticPublicKey", Puzzle("synthetic_public_key") },
        { "defaultHidden", Puzzle("default_hidden") },
        { "payToConditions", Puzzle("pay_to_conditions") },
        { "payToDelegatedOrHidden", Puzzle("pay_to_delegated_or_hidden") },
        { "delegated", Puzzle("delegated", "tails") },
        { "everythingWithSignature", Puzzle("everything_with_signature", "tails") },
        { "indexedWithSignature", Puzzle("indexed_with_signature", "tails") },
        { "genesisByCoinId", Puzzle("genesis_by_coin_id", "tails") },
        { "meltableGenesisByCoinId", Puzzle("meltable_genesis_by_coin_id", "tails") },
    };

    public static Program GetPuzzle(string name) => puzzles[name];

    private static Program Puzzle(string name, string? folder = null)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = "wallet.puzzles";
        if (folder != null)
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