using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

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
            ) { }

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
            target.Fill(new System.Windows.Int32Rect(0, 0, width, height), Colors.Black);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    var px = data[x, y];
                    target.SetPixel(
                        x,
                        y,
                        Color.FromRgb(
                            (byte)Math.Round(px.R),
                            (byte)Math.Round(px.G),
                            (byte)Math.Round(px.B)
                        )
                    );
                }

            target.UpdateWhole();
            target.UnlockDrawingContext();
        }
    }

    public class Simulator
    {
        public readonly Random rng = new();

        public class Config
        {
            public double speed = .2;
            public double angleChangeModifier = .5;
            public int iterations = 10_000;
            public double splitProbability = .0025;
            public float penIntensity = 8;
            public int initialSpores = 1;
        }

        public class Spore
        {
            static readonly Random rng = new();

            readonly Simulator sim;
            public double X { get; private set; }
            public double Y { get; private set; }
            public double Angle { get; private set; }
            public ChannelFlags AllowedChannels { get; private set; }

            public Spore(Simulator sim, double x, double y, double angle, ChannelFlags allowedChannels = ChannelFlags.All)
            {
                this.sim = sim;
                (X, Y) = (x, y);
                Angle = angle;
                AllowedChannels = allowedChannels;
            }

            public void Tick()
            {
                var img = sim.image!;

                X += sim.config.speed * Math.Cos(Angle);
                Y += sim.config.speed * Math.Sin(Angle);

                if (X < 0) X += img.width;
                if (Y < 0) Y += img.height;
                if (X >= img.width) X -= img.width;
                if (Y >= img.height) Y -= img.height;

                Angle += (rng.NextDouble() - .5) * sim.config.angleChangeModifier;
            }

            public void MakeColor()
            {
                var img = sim.image!;

                var (x1, y1) = ((int)Math.Floor(X), (int)Math.Floor(Y));
                var (x2, y2) = ((int)Math.Ceiling(X), (int)Math.Ceiling(Y));
                if (x2 >= img.width) x2 = 0;
                if (y2 >= img.height) y2 = 0;

                var (rx2, ry2) = (X - x1, Y - y1);
                var (rx1, ry1) = (1 - rx2, 1 - ry2);

                Pixel makePixel(double rx, double ry) => new((float)(sim.config.penIntensity * rx * ry), AllowedChannels);

                img[x1, y1] += makePixel(rx1, ry1);
                img[x2, y1] += makePixel(rx2, ry1);
                img[x1, y2] += makePixel(rx1, ry2);
                img[x2, y2] += makePixel(rx2, ry2);
            }

            public Spore Split() => new(sim, X, Y, Angle + Math.PI / 8, AllowedChannels);
        }

        SimulatedImage? image;
        public readonly Config config;

        public Simulator(Config? config = null) => this.config = config ?? new();

        public SimulatedImage GenerateSingleImage(int width, int height)
        {
            image = new SimulatedImage(width, height);

            var spores = new List<Spore>();

            for (int i = 0; i < config.initialSpores; i++)
            {
                //foreach (var ch in new ChannelFlags[] { ChannelFlags.Red, ChannelFlags.Green, ChannelFlags.Blue })
                    spores.Add(new Spore(
                        sim: this,
                        x: rng.Next(0, image.width),
                        y: rng.Next(0, image.height),
                        angle: rng.NextDouble() * 2 * Math.PI
                        //,allowedChannels: ch
                    ));
            }

            for (int i = 0; i < config.iterations; i++)
            {
                foreach (var spore in spores)
                {
                    spore.Tick();
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
