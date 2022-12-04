using System;
using System.Drawing;
using System.Collections.Generic;

namespace Hyphaeizer
{
    [Flags]
    public enum ChannelFlags
    {
        None = 0,
        All = Red | Green | Blue,
        Red = 0x1,
        Green = 0x2,
        Blue = 0x4
    }

    public partial class Simulator
    {
        public class Config
        {
            public double speed = 1; //.2;
            public double angleChangeModifier = .5;
            public int iterations = 1; //0_000;
            public double splitProbability = 0; // .0025;
            public float penIntensity = 255; //8;
            public int initialSpores = 1;
        }

        public struct Pixel
        {
            public float R { get; set; }
            public float G { get; set; }
            public float B { get; set; }

            public Pixel(float all) : this(all, all, all) { }

            public Pixel(float all, ChannelFlags flags)
                : this(
                      flags.HasFlag(ChannelFlags.Red) ? all : 0,
                      flags.HasFlag(ChannelFlags.Green) ? all : 0,
                      flags.HasFlag(ChannelFlags.Blue) ? all : 0
                )
            { }

            public Pixel(float red, float green, float blue)
            {
                R = red;
                G = green;
                B = blue;
            }

            public static Pixel operator +(Pixel left, Pixel right) => new(left.R + right.R, left.G + right.G, left.B + right.B);

            public static Pixel operator -(Pixel left, Pixel right) => new(left.R - right.R, left.G - right.G, left.B - right.B);

            public double Luminance => (R * 0.3) + (G * 0.59) + (B * 0.11);
        }

        public class SimulatedImage
        {
            public readonly int width, height;
            readonly Pixel[,] data;

            public SimulatedImage(int width, int height)
            {
                this.width = width;
                this.height = height;
                data = new Pixel[width, height];
            }

            public Pixel this[int x, int y]
            {
                get => data[x, y];
                set => data[x, y] = value;
            }

            public void PutOnFastWriteableBitmap(FastWriteableBitmap target)
            {
                target.LockDrawingContext();
                target.Fill(new System.Windows.Int32Rect(0, 0, width, height), Color.Black);

                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                    {
                        var px = data[x, y];
                        target.SetPixel(
                            x,
                            y,
                            Color.FromArgb(
                                (byte)Math.Clamp(px.R, 0, 255),
                                (byte)Math.Clamp(px.G, 0, 255),
                                (byte)Math.Clamp(px.B, 0, 255)
                            )
                        );
                    }

                target.UpdateWhole();
                target.UnlockDrawingContext();
            }
        }

        public readonly Random rng = new();

        Bitmap? _overlayBitmap;
        public Bitmap? OverlayBitmap
        {
            get => _overlayBitmap;

            set
            {
                _overlayBitmap = value;
            }
        }

        SimulatedImage? image;
        public readonly Config config;

        int iterationCounter = 0;

        public Simulator(Config? config = null) => this.config = config ?? new();

        public SimulatedImage GenerateSingleImage()
        {
            if (OverlayBitmap is null)
                throw new InvalidOperationException("Either set an overlay image or call the GenerateSingleImage(int,int) overload.");
            return GenerateSingleImageInternal(OverlayBitmap.Width, OverlayBitmap.Height);
        }

        public SimulatedImage GenerateSingleImage(int width, int height)
        {
            if (OverlayBitmap is not null)
                throw new InvalidOperationException("When an overlay image is set, the generated image must have the same size. " +
                    "Call the GenerateSingleImage() overload.");
            return GenerateSingleImageInternal(width, height);
        }

        SimulatedImage GenerateSingleImageInternal(int width, int height)
        {
            image = new SimulatedImage(width, height);

            var spores = new List<Spore>();

            for (int i = 0; i < config.initialSpores; i++)
            {
                spores.Add(new SightedSpore(
                    sim: this,
                    x: rng.Next(0, image.width),
                    y: rng.Next(0, image.height),
                    angle: rng.NextDouble() * 2 * Math.PI
                ));
            }

            //spores.Add(new SightedSpore(
            //    sim: this,
            //    x: 10,
            //    y: image.height / 2,
            //    angle: rng.NextDouble() * 2 * Math.PI
            //));

            for (iterationCounter = 0; iterationCounter < config.iterations; iterationCounter++)
            {
                foreach (var spore in spores)
                {
                    spore.Move();
                    spore.DecideAngle();
                    spore.MakeColor();
                }

                if (rng.NextDouble() < config.splitProbability)
                {
                    var parent = spores[rng.Next(0, spores.Count)];
                    spores.Add(parent.Split());
                }
            }

            return image;
        }
    }
}
