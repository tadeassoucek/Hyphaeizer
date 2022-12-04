using System;
using System.Linq;
using System.Drawing;

namespace Hyphaeizer
{
    public partial class Simulator
    {
        public abstract class Spore
        {
            static protected readonly Random rng = new();

            protected readonly Simulator sim;
            public double X { get; protected set; }
            public double Y { get; protected set; }
            public double Angle { get; protected set; }
            protected double Speed { get; set; }
            public ChannelFlags AllowedChannels { get; protected set; }

            public Spore(Simulator sim, double x, double y, double angle, ChannelFlags allowedChannels)
            {
                this.sim = sim;
                (X, Y) = (x, y);
                Angle = angle;
                AllowedChannels = allowedChannels;
                Speed = sim.config.speed;
            }

            public void Move()
            {
                var img = sim.image!;

                X += Speed * Math.Cos(Angle);
                Y += Speed * Math.Sin(Angle);

                if (X < 0 || X >= img.width || Y < 0 || Y >= img.height)
                    Angle += Math.PI;

                if (X < 0) X = 0;
                if (Y < 0) Y = 0;
                if (X >= img.width) X = img.width - 1;
                else if (Y >= img.height) Y = img.height - 1;
            }

            public abstract void DecideAngle();

            public void MakeColor()
            {
                var img = sim.image!;

                var (x1, y1) = ((int)Math.Floor(X), (int)Math.Floor(Y));
                var (x2, y2) = ((int)Math.Ceiling(X), (int)Math.Ceiling(Y));
                if (x2 >= img.width) x2 = 0;
                if (y2 >= img.height) y2 = 0;

#if DEBUG
                if (sim.iterationCounter == 0)
                    img[x1, y1] = new Pixel() { R = 255, G = 0, B = 0 };
#endif

                var (rx2, ry2) = (X - x1, Y - y1);
                var (rx1, ry1) = (1 - rx2, 1 - ry2);

                Pixel makePixel(double rx, double ry) => new((float)(sim.config.penIntensity * rx * ry), AllowedChannels);

                img[x1, y1] += makePixel(rx1, ry1);
                img[x2, y1] += makePixel(rx2, ry1);
                img[x1, y2] += makePixel(rx1, ry2);
                img[x2, y2] += makePixel(rx2, ry2);
            }

            public abstract Spore Split();
        }

        public class BlindSpore : Spore
        {
            public BlindSpore(Simulator sim, double x, double y, double angle, ChannelFlags allowedChannels = ChannelFlags.All)
                : base(sim, x, y, angle, allowedChannels) { }

            public override void DecideAngle() => Angle += (rng.NextDouble() - .5) * sim.config.angleChangeModifier;

            public override Spore Split() => new BlindSpore(sim, X, Y, Angle + Math.PI / 8, AllowedChannels);
        }
    
        public class SightedSpore : Spore
        {
            public SightedSpore(Simulator sim, double x, double y, double angle, ChannelFlags allowedChannels = ChannelFlags.All)
                : base(sim, x, y, angle, allowedChannels) { }

            enum Direction
            {
                North, South, West, East
            }

            public override void DecideAngle()
            {
                var ovr = sim.OverlayBitmap!;
                var (xf, yf) = ((int)Math.Floor(X), (int)Math.Floor(Y));

                if (sim.iterationCounter == 0 || (sim.iterationCounter + 1) % 50 == 0)
                {
                    int nWeight = 0, sWeight = 0, wWeight = 0, eWeight = 0;
                    //int nwWeight = 0, neWeight = 0, swWeight = 0, seWeight = 0;

                    for (int i = 0; i < yf; i++)
                        nWeight += (byte)Util.GetLuminance(ovr.GetPixel(xf, i));
                    for (int i = yf; i < ovr.Height; i++)
                        sWeight += (byte)Util.GetLuminance(ovr.GetPixel(xf, i));

                    for (int i = 0; i < xf; i++)
                        wWeight += (byte)Util.GetLuminance(ovr.GetPixel(i, yf));
                    for (int i = xf; i < ovr.Width; i++)
                        eWeight += (byte)Util.GetLuminance(ovr.GetPixel(i, yf));

                    int totalWeight = nWeight + sWeight + wWeight + eWeight;

                    var values = new(int, Direction)[]
                    {
                        (nWeight, Direction.North),
                        (sWeight, Direction.South),
                        (wWeight, Direction.West),
                        (eWeight, Direction.East),
                    };

                    var r = sim.rng.Next(totalWeight);
                    var selectedDirection = values.First(t => (r -= t.Item1) <= 0).Item2;

                    switch (selectedDirection)
                    {
                        case Direction.North:
                            Angle = 3 * Math.PI / 2;
                            break;

                        case Direction.South:
                            Angle = Math.PI / 2;
                            break;

                        case Direction.West:
                            Angle = Math.PI;
                            break;

                        case Direction.East:
                            Angle = 0;
                            break;
                    }

                    //Console.WriteLine($"Weights N={nWeight} S={sWeight} W={wWeight} E={eWeight}");
                    //Console.WriteLine("Selected direction " + Enum.GetName(typeof(Direction), selectedDirection) + " angle " + Angle);
                }

                if (Speed >= 0)
                {
                    Speed -= Util.GetLuminance(ovr.GetPixel(xf, yf)) / 100_000;
                    if (Speed < 0)
                        Speed = 0;
                }

                Angle += (rng.NextDouble() - .5) * sim.config.angleChangeModifier;
            }

            public override Spore Split() => new SightedSpore(sim, X, Y, Angle + Math.PI / 8, AllowedChannels);
        }
    }
}
