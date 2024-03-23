using chia.dotnet.bls;
using chia.dotnet.clvm;

namespace chia.dotnet.wallet;

internal static class Sign
{
    public static SpendBundle SignSpendBundle(SpendBundle spendBundle, byte[] aggSigMeExtraData, bool partial, params bls.PrivateKey[] privateKeys)
    {
        var signatures = new List<JacobianPoint>
        {
            JacobianPoint.FromHexG2(HexHelper.SanitizeHex(spendBundle.AggregatedSignature))
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

    public static JacobianPoint SignCoinSpend(
        CoinSpend coinSpend,
        byte[] aggSigMeExtraData,
        bool partial,
        params bls.PrivateKey[] privateKeys)
    {
        List<JacobianPoint> signatures = [];

        // Assuming Program and ProgramItem classes are defined elsewhere
        var conditions = Program.DeserializeHex(HexHelper.SanitizeHex(coinSpend.PuzzleReveal))
             .Run(Program.DeserializeHex(HexHelper.SanitizeHex(coinSpend.Solution)))
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
                                HexHelper.SanitizeHex(coinSpend.Coin.ParentCoinInfo).FromHex(),
                                HexHelper.SanitizeHex(coinSpend.Coin.PuzzleHash).FromHex(),
                                coinSpend.Coin.Amount.EncodeBigInt())),
                        aggSigMeExtraData));

            pairs.Add((JacobianPoint.FromBytesG1(condition[1].Atom), message));
        }

        foreach (var (publicKey, message) in pairs)
        {
            var privateKey = privateKeys.FirstOrDefault(pk => pk.GetG1().Equals(publicKey));

            if (privateKey == null)
            {
                if (partial)
                {
                    continue;
                }

                throw new Exception($"Could not find private key for {publicKey.ToHex()}.");
            }

            signatures.Add(AugSchemeMPL.Sign(privateKey, message));
        }

        return signatures.Count > 0
            ? AugSchemeMPL.Aggregate([.. signatures])
            : JacobianPoint.InfinityG2();
    }
}