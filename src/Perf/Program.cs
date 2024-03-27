using chia.dotnet.wallet;
using chia.dotnet;

var config = Config.Open();
var fullNodeEndpoint = config.GetEndpoint("full_node");
var fullNode = new FullNodeProxy(new HttpRpcClient(fullNodeEndpoint), "wallet.tests");

var walletEndpoint = config.GetEndpoint("wallet");
var walletProxy = new WalletProxy(new HttpRpcClient(walletEndpoint), "wallet.tests");
var keyStore = await KeyStore.CreateFrom(walletProxy);

var wallet = new StandardWallet(fullNode, keyStore);

await wallet.Sync();