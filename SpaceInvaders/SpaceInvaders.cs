using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SpaceInvaders;

public class SpaceInvaders : Game
{
    private const int Width = SpaceInvadersCore.SpaceInvadersCore.Width;
    private const int Height = SpaceInvadersCore.SpaceInvadersCore.Height;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpaceInvadersCore.SpaceInvadersCore core;
    private bool frameChanged = true;
    private bool drawn = true;
    private Texture2D framebuffer;
    private Stopwatch ttsw = new();
    private readonly Color[] frameBuffer = new Color[Width * Height];
    private object @lock = new();
    private bool[] _buffer;
    private readonly Color[][] colorMask = new Color[2][];
    private Sound _sound;
    public bool mute = false;

    private struct Sound
    {
        public SoundEffectInstance UFO;
        public SoundEffectInstance Shot;
        public SoundEffectInstance Flash;
        public SoundEffectInstance InvaderDie;
        public SoundEffectInstance FleetMovement1;
        public SoundEffectInstance FleetMovement2;
        public SoundEffectInstance FleetMovement3;
        public SoundEffectInstance FleetMovement4;
        public SoundEffectInstance UFOHit;
    }

    public SpaceInvaders()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        byte lastPort3 = 0;
        byte lastPort5 = 0;
        core = new SpaceInvadersCore.SpaceInvadersCore(b =>
        {
            if (!mute)
            {
                if (((b & 0b00000001) != 0) && ((lastPort3 & 0b00000001) == 0))
                {
                    _sound.UFO.Play();
                }
                else if (((b & 0b00000001) == 0) && ((lastPort3 & 0b00000001) != 0))
                {
                    if (_sound.UFO.State == SoundState.Playing)
                    {
                        _sound.UFO.Stop(true);
                    }
                }

                if (((b & 0b00000010) != 0) && ((lastPort3 & 0b00000010) == 0))
                {
                    _sound.Shot.Play();
                }

                if (((b & 0b00000100) != 0) && ((lastPort3 & 0b00000100) == 0))
                {
                    _sound.Flash.Play();
                }

                if (((b & 0b00001000) != 0) && ((lastPort3 & 0b00001000) == 0))
                {
                    _sound.InvaderDie.Play();
                }
            }

            lastPort3 = b;
        }, b =>
        {
            if (!mute)
            {
                if (((b & 0b00000001) != 0) && ((lastPort5 & 0b00000001) == 0))
                {
                    _sound.FleetMovement1.Play();
                }

                if (((b & 0b00000010) != 0) && ((lastPort5 & 0b00000010) == 0))
                {
                    _sound.FleetMovement2.Play();
                }

                if (((b & 0b00000100) != 0) && ((lastPort5 & 0b00000100) == 0))
                {
                    _sound.FleetMovement3.Play();
                }

                if (((b & 0b00001000) != 0) && ((lastPort5 & 0b00001000) == 0))
                {
                    _sound.FleetMovement4.Play();
                }

                if (((b & 0b00010000) != 0) && ((lastPort5 & 0b00010000) == 0))
                {
                    _sound.UFOHit.Play();
                }
            }

            lastPort5 = b;
        });
    }

    protected override void Initialize()
    {
        colorMask[0] = new Color[Width * Height];
        colorMask[1] = new Color[Width * Height];
        AddColorMask(0, 0, Width, Height, Color.White, Color.Black);
        AddColorMask(0, 32, Width, 48, Color.Red, Color.Black);
        var spaceInvadersGreen = new Color(0, 255, 0, 255);
        for (var i = 0; i < 4; i++)
        {
            AddColorMask(32 + i * 45, 192, 54 + i * 45, 208, spaceInvadersGreen, Color.Black);
        }

        AddColorMask(26, 240, 55, 248, spaceInvadersGreen, Color.Black);
        base.Initialize();
        return;

        void AddColorMask(int startX, int startY, int endX, int endY, Color setColor, Color unsetColor)
        {
            for (var y = startY; y < endY; y++)
            {
                for (var x = startX; x < endX; x++)
                {
                    colorMask[0][x + y * Width] = unsetColor;
                    colorMask[1][x + y * Width] = setColor;
                }
            }
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        framebuffer = new Texture2D(GraphicsDevice, Width, Height);

        core.LoadMemory(Content.Load<byte[]>("Binaries/invaders.h.bin"), 0)
            .LoadMemory(Content.Load<byte[]>("Binaries/invaders.g.bin"), 0x800)
            .LoadMemory(Content.Load<byte[]>("Binaries/invaders.f.bin"), 0x1000)
            .LoadMemory(Content.Load<byte[]>("Binaries/invaders.e.bin"), 0x1800)
            ;
        _sound.UFO = Content.Load<SoundEffect>("Sounds/ufo_lowpitch").CreateInstance();
        _sound.UFO.IsLooped = true;
        _sound.Shot = Content.Load<SoundEffect>("Sounds/shoot").CreateInstance();
        _sound.Flash = Content.Load<SoundEffect>("Sounds/explosion").CreateInstance();
        _sound.InvaderDie = Content.Load<SoundEffect>("Sounds/invaderkilled").CreateInstance();
        _sound.FleetMovement1 = Content.Load<SoundEffect>("Sounds/fastinvader1").CreateInstance();
        _sound.FleetMovement2 = Content.Load<SoundEffect>("Sounds/fastinvader2").CreateInstance();
        _sound.FleetMovement3 = Content.Load<SoundEffect>("Sounds/fastinvader3").CreateInstance();
        _sound.FleetMovement4 = Content.Load<SoundEffect>("Sounds/fastinvader4").CreateInstance();
        _sound.UFOHit = Content.Load<SoundEffect>("Sounds/explosion").CreateInstance();
    }

    protected override void BeginRun()
    {
        new Thread(() =>
        {
            var sw = Stopwatch.StartNew();
            while (core.Running)
            {
                const double cyclesPerSec = 2_000_000.0;
                const double cyclesPerFrame = cyclesPerSec / 60.0;
                const double halfCyclesPerFrame = cyclesPerFrame / 2.0;
                const double frameTime = 1000.0 / 60.0;
                const double frameTimeMicros = frameTime * 1000.0;
                const double halfFrameTimeMicros = frameTimeMicros / 0.5;
                sw.Restart();
                core.TickCpu(halfFrameTimeMicros);
                if (core.TickVideo(frameChanged) && drawn)
                {
                    _buffer = core.GetFramebuffer();
                    frameChanged = true;
                    drawn = false;
                }

                core.TickCpu(halfFrameTimeMicros - 11 * 500);
                if (core.TickVideo(frameChanged) && drawn)
                {
                    _buffer = core.GetFramebuffer();
                    frameChanged = true;
                    drawn = false;
                }

                while (!drawn && core.Running)
                {
                    core.TickCpu(1);
                }
            }
        }).Start();
        ttsw.Start();
        base.BeginRun();
    }

    protected override void EndRun()
    {
        core.Stop();
        base.EndRun();
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        if (Keyboard.GetState().IsKeyDown(Keys.M))
        {
            mute = !mute;
        }

        core.InsertCoin(Keyboard.GetState().IsKeyDown(Keys.C));
        core.SelectOnePlayer(Keyboard.GetState().IsKeyDown(Keys.D1));
        core.SelectTwoPlayer(Keyboard.GetState().IsKeyDown(Keys.D2));
        core.MoveLeftP1(Keyboard.GetState().IsKeyDown(Keys.Left));
        core.MoveRightP1(Keyboard.GetState().IsKeyDown(Keys.Right));
        core.ShootP1(Keyboard.GetState().IsKeyDown(Keys.Space));
        core.MoveLeftP2(Keyboard.GetState().IsKeyDown(Keys.A));
        core.MoveRightP2(Keyboard.GetState().IsKeyDown(Keys.D));
        core.ShootP2(Keyboard.GetState().IsKeyDown(Keys.S));
        core.Tilt(Keyboard.GetState().IsKeyDown(Keys.T));

        Window.Title = $"SpaceInvaders FPS: {gameTime.ElapsedGameTime}";
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        bool changed;
        lock (@lock)
        {
            changed = frameChanged;
        }

        if (changed)
        {
            UpdateFramebuffer();
        }

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var scale = Math.Min(GraphicsDevice.Viewport.Width / (float)framebuffer.Width,
            GraphicsDevice.Viewport.Height / (float)framebuffer.Height);
        var position = new Vector2(
            (GraphicsDevice.Viewport.Width - framebuffer.Width * scale) / 2,
            (GraphicsDevice.Viewport.Height - framebuffer.Height * scale) / 2
        );

        _spriteBatch.Draw(
            framebuffer, // Texture to draw
            position, // Position to draw at
            null, // Source rectangle (null for full texture)
            Color.White, // Color tint
            0, // Rotation angle in radians
            Vector2.Zero, // Origin for rotation
            scale, // Scale
            SpriteEffects.None, // Sprite effects
            0f // Layer depth
        );

        _spriteBatch.End();
        lock (@lock)
        {
            drawn = true;
        }

        base.Draw(gameTime);
    }

    private void UpdateFramebuffer()
    {
        lock (@lock)
        {
            frameChanged = false;
        }

        lock (@lock)
        {
            for (var y = 0; y < Width; y++)
            {
                for (var x = 0; x < Height; x++)
                {
                    frameBuffer[y + (Height - x - 1) * Width] =
                        colorMask[_buffer[x + y * Height] ? 1 : 0][y + (Height - x - 1) * Width];
                }
            }
        }


        // for (var y = 0; y < Height; y++)
        // {
        //     for (var x = 0; x < Width; x++)
        //     {
        //         var index = x / 8 + y * (Width / 8);
        //         var cell = (buffer[index] >> (7 - (x % 8))) & 0b1;
        //         cell = cell != 0 ? 255 : 0;
        //         colorBuffer[y * Width + x] = new Color(cell, cell, cell, 1.0f);
        //     }
        // }

        framebuffer.SetData(frameBuffer);
    }
}