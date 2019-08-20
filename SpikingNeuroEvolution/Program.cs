using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static System.Math;

namespace SpikingNeuroEvolution
{
    class Neuron
    {
        public readonly List<Synapse> InputSynapses = new List<Synapse>();

        private readonly double a = 0.02;
        private readonly double b = 0.2;
        private readonly double c = -65;
        private readonly double d = 2;
        private readonly double ut = 30;

        public double U { get; private set; } = -65;
        private double u2;

        public readonly List<double> Spikes = new List<double>();

        public void Update(double t, double dt, double I)
        {
            var du_dt = 0.04 * U * U + 5 * U + 140 - u2 + I;
            var du2_dt = a * (b * U - u2);

            U += dt * du_dt;
            u2 += dt * du2_dt;

            if (U >= ut && du_dt > 0)
            {
                U = c;
                u2 += d;
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

    class Synapse
    {
        private readonly Neuron _source;
        private readonly Neuron _target;
        public double Weight { get; }
        public double E { get; }

        private readonly double tDecay = 40;
        private readonly double tRise = 3;
        private readonly double gNMDA = 1.2;
        private readonly double N = 1.358;
        private readonly double Mg = 1.2;
        private readonly double a = 0.062;
        private readonly double b = 3.57;

        public double SpikesTrace(double t)
        {
            var gInf = 1 / (1 + (_target.U < -50 ? 0 : Exp(a * _target.U) * Mg / b));

            return gInf * gNMDA * N * _source.Spikes.Sum(ts => Exp((ts - t) / tDecay) - Exp((ts - t) / tRise));
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

    class NeuralNet
    {
        public readonly IReadOnlyList<Neuron> Neurons;
        
        private double t;

        public NeuralNet()
        {
            var n1 = new Neuron();
            var n2 = new Neuron();
            var n3 = new Neuron();
            var n4 = new Neuron();
            var n5 = new Neuron();
            Neurons = new List<Neuron> {n1, n2, n3, n4, n5};

            Connect(n1, n2, 0.01);
            Connect(n1, n3, 0.01);
            Connect(n2, n5, 0.1);
            Connect(n1, n4, 0.01);
            Connect(n3, n4, 0.01);
            Connect(n4, n5, 0.01);
        }

        public void Connect(Neuron source, Neuron target, double w)
        {
            var synapse = new Synapse(source, target, w, 0);
            target.InputSynapses.Add(synapse);
        }

        public void Update(double dt, IReadOnlyDictionary<Neuron, double> additionalCurrent)
        {
            var inputs = Neurons.ToDictionary(neuron => neuron,
                neuron => neuron.InputCurrent(t, additionalCurrent[neuron]));

            foreach (var neuron in Neurons)
            {
                double inputCurrent = inputs[neuron];

                neuron.Update(t, dt, inputCurrent);
            }

            t += dt;
        }

        public void PrintNet(IReadOnlyDictionary<Neuron, double> additionalCurrent)
        {
            Console.WriteLine("I");
            foreach (var neuron in Neurons)
            {
                F(neuron.InputCurrent(t, additionalCurrent[neuron]));
            }

            Console.WriteLine("U+65");
            foreach (var neuron in Neurons)
            {
                F(neuron.U + 65);
            }

            Console.WriteLine("Spikes");
            foreach (var neuron in Neurons)
            {
                F(neuron.Spikes.Count);
            }

            Console.WriteLine();
        }

        public static void F(double d)
        {
            Console.WriteLine(d);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var nn = new NeuralNet();

            var additionalCurrent = nn.Neurons.ToDictionary(neuron => neuron, neuron => 0.0);
            additionalCurrent[nn.Neurons[0]] = 20;

            while (true)
            {
                for (int i = 0; i < 100; i++)
                {
                    nn.Update(0.01, additionalCurrent);
                }

                Console.Clear();
                nn.PrintNet(additionalCurrent);
                Thread.Sleep(10);
            }
        }
    }
}
