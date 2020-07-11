using System.Collections.Generic;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class Neuron
    {
        public readonly List<Synapse> InputSynapses = new List<Synapse>();

        private readonly double a = 0.02;
        private readonly double b = 0.2;
        private readonly double c = -50;
        private readonly double d = 2;
        private readonly double ut = 30;

        public double U { get; private set; } = -65;
        public double U2;

        public readonly List<double> Spikes = new List<double>();

        public void Update(double t, double dt, double I)
        {
            // https://www.simbrain.net/Documentation/docs/Pages/Network/neuron/Izhikevich.html
            var du_dt = 0.04 * U * U + 5 * U + 140 - U2 + I;
            var du2_dt = a * (b * U - U2);

            U += dt * du_dt;
            U2 += dt * du2_dt;

            if (U >= ut && du_dt > 0)
            {
                U = c;
                U2 += d;
                Spikes.Add(t);
            }
        }

        public double SynapticStimulation(double t)
        {
            return InputSynapses.Sum(synapse => synapse.SynapseInput(t));
        }

        public double InputCurrent(double t, double externalInput)
        {
            return externalInput + SynapticStimulation(t);
        }
    }
}
