using chia.dotnet.bls;
using dotnetstandard_bip39;

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
    public G1Element PublicKey { get; init; }

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
    /// <param name="publicKey">The key to initialize the key store with.</param>
    /// <param name="hardened">A value indicating whether the key store is hardened.</param>
    /// <exception cref="ArgumentException">Thrown when the key is neither a PrivateKey nor a JacobianPoint.</exception>
    public KeyStore(G1Element publicKey, bool hardened = false)
    {
        PrivateKey = null;
        PublicKey = publicKey;

        Hardened = hardened;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyStore"/> class.
    /// </summary>
    /// <param name="privateKey">The key to initialize the key store with.</param>
    /// <param name="hardened">A value indicating whether the key store is hardened.</param>
    /// <exception cref="ArgumentException">Thrown when the key is neither a PrivateKey nor a JacobianPoint.</exception>
    public KeyStore(bls.PrivateKey privateKey, bool hardened = false)
    {
        PrivateKey = privateKey;
        PublicKey = privateKey.GetG1Element();

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
        G1Element publicKey;

        if (Hardened || PrivateKey != null)
        {
            bls.PrivateKey rootPrivateKey = PrivateKey ?? throw new InvalidOperationException("Cannot generate private key without root private key.");
            privateKey = KeyDerivation.DerivePrivateKey(rootPrivateKey, index, Hardened);
            publicKey = privateKey.Value.GetG1Element();
        }
        else
        {
            privateKey = null;
            publicKey = KeyDerivation.DerivePublicKeyWallet(PublicKey, index);
        }

        return new KeyPair(publicKey, privateKey);
    }

    /// <summary>
    /// Creates a new <see cref="KeyStore"/> instance from a <see cref="WalletProxy"/>.
    /// </summary>
    /// <param name="walletProxy">The wallet proxy to create the key store from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>Uses the currently logged in fingerprint</remarks>
    /// <returns>A new <see cref="KeyStore"/> instance.</returns>
    public static async Task<KeyStore> CreateFrom(WalletProxy walletProxy, CancellationToken cancellationToken = default)
    {
        var fingerprint = await walletProxy.GetLoggedInFingerprint(cancellationToken) ?? throw new Exception("No wallet found");
        return await CreateFrom(walletProxy, fingerprint, cancellationToken);
    }

    /// <summary>
    /// Creates a new <see cref="KeyStore"/> instance from a <see cref="WalletProxy"/> and a fingerprint.
    /// </summary>
    /// <param name="walletProxy">The wallet proxy to create the key store from.</param>
    /// <param name="fingerprint">The fingerprint to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <remarks>The logged in fingerprint is not changed.</remarks>
    /// <returns>A new <see cref="KeyStore"/> instance.</returns>
    public static async Task<KeyStore> CreateFrom(WalletProxy walletProxy, uint fingerprint, CancellationToken cancellationToken = default)
    {
        var privateKeyInfo = await walletProxy.GetPrivateKey(fingerprint, cancellationToken);
        return CreateFrom(privateKeyInfo.Seed);
    }

    /// <summary>
    /// Creates a new <see cref="KeyStore"/> instance from a mnemonic phrase.
    /// </summary>
    /// <param name="mnemonic">The mnemonic phrase.</param>
    /// <returns>A new <see cref="KeyStore"/> instance.</returns>
    public static KeyStore CreateFrom(string mnemonic)
    {
        var bip39 = new BIP39();
        var seed = bip39.MnemonicToSeedHex(mnemonic, "");
        var sk = bls.PrivateKey.FromSeed(seed);
        return new KeyStore(sk);
    }

    /// <summary>
    /// Creates a new <see cref="KeyStore"/> instance from a mnemonic phrase.
    /// </summary>
    /// <param name="seed">The seed.</param>
    /// <returns>A new <see cref="KeyStore"/> instance.</returns>
    public static KeyStore CreateFrom(byte[] seed)
    {
        var sk = bls.PrivateKey.FromSeed(seed);
        return new KeyStore(sk);
    }
}
