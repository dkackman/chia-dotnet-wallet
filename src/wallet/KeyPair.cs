using chia.dotnet.bls;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents a key pair consisting of a public key and an optional private key.
/// </summary>
public record KeyPair
{
    /// <summary>
    /// Gets or sets the public key.
    /// </summary>
    public JacobianPoint PublicKey { get; init; }

    /// <summary>
    /// Gets or sets the private key. Can be null if the private key is not available.
    /// </summary>
    public bls.PrivateKey? PrivateKey { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyPair"/> class.
    /// </summary>
    /// <param name="publicKey">The public key.</param>
    /// <param name="privateKey">The private key. Can be null.</param>
    public KeyPair(JacobianPoint publicKey, bls.PrivateKey? privateKey = null)
    {
        PublicKey = publicKey;
        PrivateKey = privateKey;
    }
}
