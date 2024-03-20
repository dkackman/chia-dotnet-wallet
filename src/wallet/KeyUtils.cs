using chia.dotnet.bls;

namespace chia.dotnet.wallet;

internal static class KeyUtils
{
    public static bls.PrivateKey DerivePrivateKey(bls.PrivateKey masterPrivateKey, int index, bool hardened) => DerivePrivateKeyPath(
            masterPrivateKey,
            [12381, 8444, 2, index],
            hardened
        );

    public static bls.PrivateKey DerivePrivateKeyPath(bls.PrivateKey privateKey, int[] path, bool hardened)
    {
        foreach (var index in path)
        {
            privateKey = hardened
                ? AugSchemeMPL.DeriveChildSk(privateKey, index)
                : AugSchemeMPL.DeriveChildSkUnhardened(privateKey, index);
        }
        return privateKey;
    }

    public static JacobianPoint DerivePublicKeyPath(JacobianPoint publicKey, int[] path)
    {
        foreach (var index in path)
        {
            publicKey = AugSchemeMPL.DeriveChildPkUnhardened(publicKey, index);
        }
        return publicKey;
    }

    public static JacobianPoint DerivePublicKey(JacobianPoint masterPublicKey, int index) => DerivePublicKeyPath(masterPublicKey, [12381, 8444, 2, index]);
}
