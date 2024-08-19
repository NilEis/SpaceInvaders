using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SpaceInvaders;

public class SpaceInvaders : Game
{
    private const int Width = SpaceInvadersCore.SpaceInvadersCore.Width;
    private const int Height = SpaceInvadersCore.SpaceInvadersCore.Height;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpaceInvadersCore.SpaceInvadersCore core = new();
    private bool frameChanged = true;
    private bool drawn = true;
    private Texture2D framebuffer;
    uint tt = 0;
    private Stopwatch ttsw = new();
    private readonly Color[] frameBuffer = new Color[Width * Height];
    private object @lock = new();
    private bool[] _buffer;

    public SpaceInvaders()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
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
    }

    protected override void BeginRun()
    {
        new Thread(() =>
        {
            var sw = Stopwatch.StartNew();
            while (core.Running)
            {
                sw.Restart();
                const double frameTime = 1000.0 / 60.0;
                const double halfFrameTimeMicros = (frameTime / 0.5) * 1000;
                core.TickCpu(halfFrameTimeMicros);
                var tickVideo = core.TickVideo(frameChanged);
                core.TickCpu(halfFrameTimeMicros);
                lock (@lock)
                {
                    frameChanged = tickVideo;
                    if (frameChanged)
                    {
                        _buffer = core.GetFramebuffer();
                        drawn = false;
                    }
                }

                SpinWait.SpinUntil(() =>
                {
                    lock (@lock)
                    {
                        return drawn;
                    }
                }, 16);
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

        core.InsertCoin(Keyboard.GetState().IsKeyDown(Keys.C));

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

        _spriteBatch.Begin();

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
                    frameBuffer[y + (Height - x - 1) * Width] = _buffer[x + y * Height] ? Color.White : Color.Black;
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