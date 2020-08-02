using System;

namespace SpikingNeuroEvolution
{
    public static class RandomExtensions
    {
        public static double NextGaussian(this Random rnd, double mean = 0, double stdDev = 1) 
        {
            double u1 = 1.0 - rnd.NextDouble();
            double u2 = 1.0 - rnd.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }
    }
}
