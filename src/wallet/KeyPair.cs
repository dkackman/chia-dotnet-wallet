using chia.dotnet.bls;

namespace chia.dotnet.wallet;

public record KeyPair
{
    public JacobianPoint PublicKey { get; init; }
    public bls.PrivateKey? PrivateKey { get; init; } 

    public KeyPair(JacobianPoint publicKey, bls.PrivateKey? privateKey = null)
    {
        PublicKey = publicKey;
        PrivateKey = privateKey;
    }
}
