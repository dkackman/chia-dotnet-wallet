using dotnetstandard_bip39;
using chia.dotnet;
using chia.dotnet.wallet;

namespace wallet.tests;

public class StandardWalletTests
{
    [Fact]
    public void Construct()
    {
        var mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var bip39 = new BIP39();
        var seed = bip39.MnemonicToSeedHex(mnemonic, "");
        var sk = chia.dotnet.bls.PrivateKey.FromSeed(seed);
        var keyStore = new KeyStore(sk);

        var config = Config.Open();
        var fullNodeEndpoint = config.GetEndpoint("full_node");
        var fullNode = new FullNodeProxy(new HttpRpcClient(fullNodeEndpoint), "wallet.tests");
        var wallet = new StandardWallet(fullNode, keyStore);
    }
}