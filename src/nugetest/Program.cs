// See https://aka.ms/new-console-template for more information

using chia.dotnet.bls;

byte[] seed =
[
    0,
        50,
        6,
        244,
        24,
        199,
        1,
        25,
        52,
        88,
        192,
        19,
        18,
        12,
        89,
        6,
        220,
        18,
        102,
        58,
        209,
        82,
        12,
        62,
        89,
        110,
        182,
        9,
        44,
        20,
        254,
        22,
    ];
byte[] message = [1, 2, 3, 4, 5];


var sk = AugSchemeMPL.KeyGen(seed);
var pk = sk.GetG1Element();
var signature = AugSchemeMPL.Sign(sk, message);
Console.WriteLine(AugSchemeMPL.Verify(pk, message, signature));
