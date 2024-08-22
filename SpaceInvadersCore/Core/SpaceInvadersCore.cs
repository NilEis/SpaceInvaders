using System;
using System.Threading.Tasks;
using Intel8080Tools;

namespace SpaceInvadersCore.Core;

public class SpaceInvadersCore
{
    public enum Sounds
    {
        UFO,
        SHOT,
        FLASH,
        INVADER_DIE,
        FLEET_MOVEMENT1,
        FLEET_MOVEMENT2,
        FLEET_MOVEMENT3,
        FLEET_MOVEMENT4,
        UFO_HIT
    }

    public readonly Intel8080 Cpu = new(0x4000);
    public const int Width = 224;
    public const int Height = 256;

    private class VideoChip
    {
        public readonly bool[] FrameBuffer = new bool[Width * Height];
        public bool HalfInterrupt = true;
    }

    private readonly ShiftRegister _sR = new();
    private readonly VideoChip _videoChip = new();

    public bool Running = true;

    public SpaceInvadersCore(Action<byte> soundCallbackPort3, Action<byte> soundCallbackPort5)
    {
        TaskFactory fac = new();
        Cpu.SetPin(Pin.INTE, false);
        Cpu.SetInPort(0, _ => 0b01110000);
        Cpu.SetInPort(1, _ => (byte)(Cpu.Ports[1] | 0b00001000));
        Cpu.SetInPort(2, _ => Cpu.Ports[2]);
        Cpu.SetInPort(3, _ => _sR.Reg);
        Cpu.SetOutPort(2, (_, b) => { _sR.Offset = (byte)(b & 0b00000111); });
        Cpu.SetOutPort(4, (_, b) => { _sR.Reg = b; });
        Cpu.SetOutPort(3, (_, b) => soundCallbackPort3(b));
        Cpu.SetOutPort(5, (_, b) => soundCallbackPort5(b));
        Cpu.SetOutPort(6, (_, b) =>
        {
            /* watchdog */
        });
    }

    public SpaceInvadersCore LoadMemory(byte[] mem, int offset)
    {
        Cpu.LoadMemory(mem, offset);
        return this;
    }

    public uint TickCpu(double dt)
    {
        uint t = 0;
        while (t * 500 < dt)
        {
            t += Cpu.run(cpudiag: false, safe: false, print_debug: false).Item2;
        }

        return t;
    }

    public bool[] GetFramebuffer()
    {
        return _videoChip.FrameBuffer;
    }

    public bool TickVideo(bool frameChanged)
    {
        if (!Cpu.GetPin(Pin.INTE))
        {
            return frameChanged;
        }


        var halfInterrupt = _videoChip.HalfInterrupt;
        _videoChip.HalfInterrupt = !_videoChip.HalfInterrupt;
        if (halfInterrupt)
        {
            Cpu.GenerateInterrupt(1);
            return false;
        }

        Cpu.GenerateInterrupt(2);
        var seg = Cpu.GetMemory(0x2400, 0x3FFF);
        for (var i = 0; i < seg.Count; i++)
        {
            var y = i / 32;
            for (var b = 0; b < 8; b++)
            {
                var x = i % 32 * 8 + b;
                var color = (seg[i] & 0x1 << b) != 0;
                _videoChip.FrameBuffer[x + y * Height] = color;
            }
        }

        return true;
        // var bmp = BitmapSource.Create(height, width, 0, 0, PixelFormats.BlackWhite, null, buffer,
        //     height / 8);
    }

    public void Stop()
    {
        Running = false;
    }

    public void Tilt(bool pressed)
    {
        Cpu.Ports[2] = (byte)((Cpu.Ports[2] & 0b11111011) | (pressed ? 0b00000100 : 0b00000000));
    }

    public void MoveRightP1(bool pressed)
    {
        Cpu.Ports[1] = (byte)((Cpu.Ports[1] & 0b10111111) | (pressed ? 0b01000000 : 0b00000000));
    }

    public void MoveLeftP1(bool pressed)
    {
        Cpu.Ports[1] = (byte)((Cpu.Ports[1] & 0b11011111) | (pressed ? 0b00100000 : 0b00000000));
    }

    public void ShootP1(bool pressed)
    {
        Cpu.Ports[1] = (byte)((Cpu.Ports[1] & 0b11101111) | (pressed ? 0b00010000 : 0b00000000));
    }

    public void MoveRightP2(bool pressed)
    {
        Cpu.Ports[2] = (byte)((Cpu.Ports[2] & 0b10111111) | (pressed ? 0b01000000 : 0b00000000));
    }

    public void MoveLeftP2(bool pressed)
    {
        Cpu.Ports[2] = (byte)((Cpu.Ports[2] & 0b11011111) | (pressed ? 0b00100000 : 0b00000000));
    }

    public void ShootP2(bool pressed)
    {
        Cpu.Ports[2] = (byte)((Cpu.Ports[2] & 0b11101111) | (pressed ? 0b00010000 : 0b00000000));
    }

    public void SelectOnePlayer(bool pressed)
    {
        Cpu.Ports[1] = (byte)((Cpu.Ports[1] & 0b11111011) | (pressed ? 0b00000100 : 0b00000000));
    }

    public void SelectTwoPlayer(bool pressed)
    {
        Cpu.Ports[1] = (byte)((Cpu.Ports[1] & 0b11111101) | (pressed ? 0b00000010 : 0b00000000));
    }

    public void InsertCoin(bool pressed)
    {
        Cpu.Ports[1] = (byte)((Cpu.Ports[1] & 0b11111110) | (pressed ? 0b00000001 : 0b00000000));
    }
}