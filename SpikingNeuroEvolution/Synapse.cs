using System.Linq;
using static System.Math;

namespace SpikingNeuroEvolution
{
    record class Synapse(Neuron Source, Neuron Target, double Weight, double E)
    {
        private static readonly double tDecay = 40; // ms
        private static readonly double tRise = 3;
        private static readonly double gNMDA = 1.2; // nS
        private static readonly double N = 1.358;
        private static readonly double Mg = 1.2;
        private static readonly double a = 0.062;
        private static readonly double b = 3.57;

        public double SpikesTrace(double t) => GInf * gNMDA * N * Source.Spikes.Sum(ts => SpikeTrace(t, ts));

        // Spiking Neuron Models: page 53
        private double GInf => 1 / (1 + (Target.U < -50 ? 0 : Exp(a * Target.U) * Mg / b));

        private double SpikeTrace(double t, double ts)
        {
            var dt = ts - t;
            return Exp(dt / tDecay) - Exp(dt / tRise);
        }

        public double SynapseInput(double t) => Weight * (E - Target.U) * SpikesTrace(t);
    }
}
