using System.Linq;
using static System.Math;

namespace SpikingNeuroEvolution
{
    class Synapse
    {
        private readonly Neuron _source;
        private readonly Neuron _target;
        public double Weight { get; }
        public double E { get; }

        private readonly double tDecay = 40; // ms
        private readonly double tRise = 3;
        private readonly double gNMDA = 1.2; // nS
        private readonly double N = 1.358;
        private readonly double Mg = 1.2;
        private readonly double a = 0.062;
        private readonly double b = 3.57;

        public double SpikesTrace(double t)
        {
            // Spiking Neuron Models: page 53
            var gInf = 1 / (1 + (_target.U < -50 ? 0 : Exp(a * _target.U) * Mg / b));

            return gInf * gNMDA * N * _source.Spikes.Sum(ts => SpikeTrace(t, ts));
        }

        private double SpikeTrace(double t, double ts)
        {
            var dt = ts - t;
            return Exp(dt / tDecay) - Exp(dt / tRise);
        }

        public double SynapseInput(double t)
        {
            return Weight * (E - _target.U) * SpikesTrace(t);
        }

        public Synapse(Neuron source, Neuron target, double weight, double e)
        {
            _source = source;
            _target = target;
            Weight = weight;
            E = e;
        }
    }
}
