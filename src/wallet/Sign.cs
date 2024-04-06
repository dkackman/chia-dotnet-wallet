using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

internal static class Sign
{
    public static SpendBundle SignSpendBundle(SpendBundle spendBundle, byte[] aggSigMeExtraData, bool partial, params bls.PrivateKey[] privateKeys)
    {

        var signatures = new List<G2Element>
        {
            (G2Element)JacobianPoint.FromHex(spendBundle.AggregatedSignature.Remove0x())
        };

        foreach (var coinSpend in spendBundle.CoinSpends)
        {
            signatures.Add(SignCoinSpend(coinSpend, aggSigMeExtraData, partial, privateKeys));
        }

        return new SpendBundle
        {
            CoinSpends = spendBundle.CoinSpends,
            AggregatedSignature = AugSchemeMPL.Aggregate([.. signatures]).ToHex()
        };
    }

    public static G2Element SignCoinSpend(
        CoinSpend coinSpend,
        byte[] aggSigMeExtraData,
        bool partial,
        params bls.PrivateKey[] privateKeys)
    {
        List<G2Element> signatures = [];

        // Assuming Program and ProgramItem classes are defined elsewhere
        var conditions = Program.DeserializeHex(coinSpend.PuzzleReveal.Remove0x())
             .Run(Program.DeserializeHex(coinSpend.Solution.Remove0x()))
             .Value.ToList();

        List<(JacobianPoint, byte[])> pairs = [];

        foreach (var item in conditions.Where(condition => condition.First.IsAtom && condition.First.ToInt() is 49 or 50))
        {
            var condition = item.ToList();

            if (condition.Count != 3)
                throw new Exception("Invalid condition length.");
            else if (!condition[1].IsAtom || condition[1].Atom.Length != 48)
                throw new Exception("Invalid public key.");
            else if (!condition[2].IsAtom || condition[2].Atom.Length > 1024)
                throw new Exception("Invalid message.");

            byte[] message = ByteUtils.ConcatenateArrays(
                condition[2].Atom,
                condition[0].ToInt() == 49
                    ? []
                    : ByteUtils.ConcatenateArrays(
                        Hmac.Hash256(
                            ByteUtils.ConcatenateArrays(
                                coinSpend.Coin.ParentCoinInfo.Remove0x().ToHexBytes(),
                                coinSpend.Coin.PuzzleHash.Remove0x().ToHexBytes(),
                                coinSpend.Coin.Amount.Encode())),
                        aggSigMeExtraData));

            pairs.Add((JacobianPoint.FromBytes(condition[1].Atom), message));
        }

        foreach (var (publicKey, message) in pairs)
        {
            if (!privateKeys.Any(pk => pk.GetG1Element().Equals(publicKey)))
            {
                if (partial)
                {
                    continue;
                }

                throw new Exception($"Could not find private key for {publicKey.ToHex()}.");
            }

            signatures.Add(AugSchemeMPL.Sign(privateKeys.First(pk => pk.GetG1Element().Equals(publicKey)), message));
        }

        return signatures.Count > 0
            ? AugSchemeMPL.Aggregate([.. signatures])
            : G2Element.GetInfinity();
    }
}