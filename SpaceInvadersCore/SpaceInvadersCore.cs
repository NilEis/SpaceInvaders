using System.Diagnostics;
using Intel8080Tools;
using SpaceInvaders;

namespace SpaceInvadersCore;

public class SpaceInvadersCore
{
    public readonly Intel8080 _cpu = new(0x4000);
    public const int Width = 224;
    public const int Height = 256;

    private class VideoChip
    {
        public bool[] FrameBuffer = new bool[Width * Height];
        public bool HalfInterrupt = true;
    }

    private ShiftRegister sR = new();
    private VideoChip _videoChip = new();

    public bool Running = true;

    public SpaceInvadersCore()
    {
        _cpu.SetPin(Pin.INTE, false);
        _cpu.SetInPort(0, _ => 0b01110000);
        _cpu.SetInPort(1, _ => (byte)(_cpu.Ports[1] | 0b00001000));
        _cpu.SetInPort(2, _ => 0b00000000);
        _cpu.SetInPort(3, _ => sR.Reg);
        _cpu.SetOutPort(2, (_, b) => { sR.Offset = (byte)(b & 0b00000111); });
        _cpu.SetOutPort(4, (_, b) => { sR.Reg = b; });
        _cpu.SetOutPort(3, (_, b) =>
        {
            /* sounds */
        });
        _cpu.SetOutPort(5, (_, b) =>
        {
            /* sounds */
        });
        _cpu.SetOutPort(6, (_, b) =>
        {
            /* watchdog */
        });
    }

    public SpaceInvadersCore LoadMemory(byte[] mem, int offset)
    {
        _cpu.LoadMemory(mem, offset);
        return this;
    }

    public uint TickCpu(double dt)
    {
        uint t = 0;
        while (t * 500 < dt)
        {
            t += _cpu.run(cpudiag: false, safe: false, print_debug: false).Item2;
        }

        return t;
    }

    public bool[] GetFramebuffer()
    {
        return _videoChip.FrameBuffer.ToArray();
    }

    public bool TickVideo(bool frameChanged)
    {
        if (!_cpu.GetPin(Pin.INTE))
        {
            return frameChanged;
        }


        var halfInterrupt = _videoChip.HalfInterrupt;
        _videoChip.HalfInterrupt = !_videoChip.HalfInterrupt;
        if (halfInterrupt)
        {
            _cpu.GenerateInterrupt(1);
            return false;
        }

        _cpu.GenerateInterrupt(2);
        var seg = _cpu.GetMemory(0x2400, 0x3FFF);
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

    public void MoveRightP1(bool pressed)
    {
        _cpu.Ports[1] = (byte)((_cpu.Ports[1] & 0b10111111) | (pressed ? 0b01000000 : 0b00000000));
    }

    public void MoveLeftP1(bool pressed)
    {
        _cpu.Ports[1] = (byte)((_cpu.Ports[1] & 0b11011111) | (pressed ? 0b00100000 : 0b00000000));
    }

    public void Shoot(bool pressed)
    {
        _cpu.Ports[1] = (byte)((_cpu.Ports[1] & 0b11101111) | (pressed ? 0b00010000 : 0b00000000));
    }

    public void SelectOnePlayer(bool pressed)
    {
        _cpu.Ports[1] = (byte)((_cpu.Ports[1] & 0b11111011) | (pressed ? 0b00000100 : 0b00000000));
    }

    public void InsertCoin(bool pressed)
    {
        _cpu.Ports[1] = (byte)((_cpu.Ports[1] & 0b11111110) | (pressed ? 0b00000001 : 0b00000000));
    }
}