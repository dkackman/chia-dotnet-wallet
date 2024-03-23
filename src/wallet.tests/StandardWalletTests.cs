using chia.dotnet;
using chia.dotnet.clvm;
using chia.dotnet.wallet;

namespace wallet.tests;

public class StandardWalletTests
{
    [SkippableFact]
    public void Construct()
    {
        Skip.If(Environment.GetEnvironmentVariable("CHIA_ROOT") is null);

        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var keyStore = KeyStore.CreateFrom(mnemonic);

        var config = Config.Open();
        var fullNodeEndpoint = config.GetEndpoint("full_node");
        var fullNode = new FullNodeProxy(new HttpRpcClient(fullNodeEndpoint), "wallet.tests");
        var wallet = new StandardWallet(fullNode, keyStore);

        Assert.NotNull(wallet);
    }

    [SkippableFact]
    public async Task GetLauncherCoinRecords()
    {
        Skip.If(Environment.GetEnvironmentVariable("CHIA_ROOT") is null);

        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var keyStore = KeyStore.CreateFrom(mnemonic);

        var config = Config.Open();
        var fullNodeEndpoint = config.GetEndpoint("full_node");
        var fullNode = new FullNodeProxy(new HttpRpcClient(fullNodeEndpoint), "wallet.tests");
        var wallet = new StandardWallet(fullNode, keyStore);

        await wallet.Sync();

        var launcher = Program.FromHex("1c540993becbd9ade831631e908eef720f4ba0c8262f4ed4f5e0e6bd0a57cb8a");
        var hint = Program.FromBigInt(launcher.ToBigInt() + 1)
            .ToHex()
            .PadLeft(64, '0')[..64];

        var coinRecords = await fullNode.GetCoinRecordsByHint(hint, true);
        Assert.NotEmpty(coinRecords);
    }
}