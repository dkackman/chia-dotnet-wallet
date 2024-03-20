using chia.dotnet.bls;

namespace chia.dotnet.wallet;

/// <summary>
/// Represents a key store that holds private and public keys.
/// </summary>
public class KeyStore
{
    /// <summary>
    /// Gets or sets the private key.
    /// </summary>
    public bls.PrivateKey? PrivateKey { get; init; }

    /// <summary>
    /// Gets or sets the public key.
    /// </summary>
    public JacobianPoint PublicKey { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the key store is hardened.
    /// </summary>
    public bool Hardened { get; init; }

    /// <summary>
    /// Gets or sets the list of key pairs.
    /// </summary>
    public List<KeyPair> Keys { get; init; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyStore"/> class.
    /// </summary>
    /// <param name="key">The key to initialize the key store with.</param>
    /// <param name="hardened">A value indicating whether the key store is hardened.</param>
    /// <exception cref="ArgumentException">Thrown when the key is neither a PrivateKey nor a JacobianPoint.</exception>
    public KeyStore(object key, bool hardened = false)
    {
        if (key is bls.PrivateKey privateKey)
        {
            PrivateKey = privateKey;
            PublicKey = privateKey.GetG1();
        }
        else if (key is JacobianPoint publicKey)
        {
            PrivateKey = null;
            PublicKey = publicKey;
        }
        else
        {
            throw new ArgumentException("Key must be either PrivateKey or JacobianPoint", nameof(key));
        }

        Hardened = hardened;
    }

    /// <summary>
    /// Generates the specified number of key pairs and adds them to the key store.
    /// </summary>
    /// <param name="amount">The number of key pairs to generate.</param>
    public void Generate(int amount)
    {
        int targetLength = Keys.Count + amount;

        for (int i = Keys.Count; i < targetLength; i++)
        {
            Keys.Add(GenerateKeyPair(i));
        }
    }

    /// <summary>
    /// Generates key pairs until the specified number of key pairs is reached.
    /// </summary>
    /// <param name="amount">The number of key pairs to generate.</param>
    public void GenerateUntil(int amount)
    {
        int generateAmount = amount - Keys.Count;

        if (generateAmount > 0)
        {
            Generate(generateAmount);
        }
    }

    private KeyPair GenerateKeyPair(int index)
    {
        bls.PrivateKey? privateKey;
        JacobianPoint publicKey;

        if (Hardened || PrivateKey != null)
        {
            bls.PrivateKey? rootPrivateKey = PrivateKey ?? throw new InvalidOperationException("Cannot generate private key without root private key.");
            privateKey = KeyDerivation.DerivePrivateKey(rootPrivateKey, index, Hardened);
            publicKey = privateKey.GetG1();
        }
        else
        {
            privateKey = null;
            publicKey = KeyDerivation.DerivePublicKeyWallet(PublicKey, index);
        }

        return new KeyPair(publicKey, privateKey);
    }
}
