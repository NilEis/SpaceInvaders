using System;

namespace SpaceInvadersCore.Core;

public class ShiftRegister
{
    private ushort _reg;

    public byte Reg
    {
        get
        {
            var res = _reg << Offset;
            res = (ushort)(res & 0xFF00);
            return (byte)(res >> 8);
        }
        set
        {
            var tmpA = (_reg >> 8) & 0x00FF;
            _reg = (ushort)((value << 8) | tmpA);
        }
    }

    public byte Offset
    {
        get => _offset;

        set
        {
            if (value > 0x07)
            {
                throw new ArgumentException("Offset must be less than or equal to 0x07");
            }

            _offset = value;
        }
    }

    private byte _offset;
}