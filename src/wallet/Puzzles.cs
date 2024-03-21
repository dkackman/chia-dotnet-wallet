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
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException();
        var filePath = Path.Combine(assemblyLocation, "puzzles");
        if (folder != null)
        {
            filePath = Path.Combine(filePath, folder);
        }

        filePath = Path.Combine(filePath, $"{name}.clvm.hex");

        var fileContent = File.ReadAllText(filePath).Trim();

        return Program.DeserializeHex(fileContent);
    }
}