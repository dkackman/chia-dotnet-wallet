using chia.dotnet.bls;

namespace chia.dotnet.wallet;

public class KeyStore
{
    public bls.PrivateKey? PrivateKey { get; }
    public bls.JacobianPoint PublicKey { get; }

    public bool Hardened { get; }

    public List<KeyPair> Keys { get; } = [];

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

    public void Generate(int amount)
    {
        int targetLength = Keys.Count + amount;

        for (int i = Keys.Count; i < targetLength; i++)
        {
            Keys.Add(GenerateKeyPair(i));
        }
    }

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
            privateKey = KeyUtils.DerivePrivateKey(rootPrivateKey, index, Hardened);
            publicKey = privateKey.GetG1();
        }
        else
        {
            privateKey = null;
            publicKey = KeyUtils.DerivePublicKey(PublicKey, index);
        }

        return new KeyPair(publicKey, privateKey);
    }

}
