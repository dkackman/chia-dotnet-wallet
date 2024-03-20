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
        { "tails", Puzzle("tails") },
        { "delegated", Puzzle("tails", "delegated") },
        { "everythingWithSignature", Puzzle("tails", "everything_with_signature") },
        { "indexedWithSignature", Puzzle("tails", "indexed_with_signature") },
        { "genesisByCoinId", Puzzle("tails", "genesis_by_coin_id") },
        { "meltableGenesisByCoinId", Puzzle("tails", "meltable_genesis_by_coin_id") },
    };

    public static Program GetPuzzle(string name) => puzzles[name];

    private static Program Puzzle(params string[] name)
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException();
        var filePath = Path.Combine(assemblyLocation, "puzzles");
        filePath = Path.Combine(name.Take(name.Length - 1).Append(filePath).ToArray());
        filePath = Path.Combine(filePath, $"{name.Last()}.clvm.hex");

        var fileContent = File.ReadAllText(filePath).Trim();

        return Program.DeserializeHex(fileContent);
    }
}