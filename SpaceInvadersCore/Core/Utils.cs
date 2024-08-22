namespace SpaceInvadersCore.Core;

public static class Utils
{
    private static readonly byte[] ReverseLookup =
    [
        0x0, 0x8, 0x4, 0xc, 0x2, 0xa, 0x6, 0xe,
        0x1, 0x9, 0x5, 0xd, 0x3, 0xb, 0x7, 0xf
    ];

    public static byte Reverse(byte n)
    {
        return (byte)((ReverseLookup[n & 0b1111] << 4) | ReverseLookup[n >> 4]);
    }
}