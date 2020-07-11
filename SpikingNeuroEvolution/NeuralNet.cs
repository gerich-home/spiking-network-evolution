using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class NeuralNet
    {
        public IReadOnlyList<Neuron> Neurons;

        public double T { get; private set; }

        public NeuralNet()
        {
        }

        public NeuralNet(IEnumerable<Neuron> neurons)
        {
            Neurons = neurons.ToList();
        }

        public void Initialize()
        {
            var n1 = new Neuron();
            var n2 = new Neuron();
            var n3 = new Neuron();
            Neurons = new List<Neuron> {n1, n2, n3};

            var d = 0.07;
            Connect(n1, n3, 0.01);
            Connect(n2, n3, 0.02);
            // Connect(n3, n4, 1 * 0.01); // 0.045 -> 0.01
            // Connect(n4, n5, 1 * d);
            // Connect(n5, n6, 1 * d);
            // Connect(n6, n7, 1 * d);
            // Connect(n7, n8, 1 * d);
            // Connect(n8, n9, 1 * d);
            //Connect(n1, n4, 0.01);
            //Connect(n3, n4, 0.01);
            //Connect(n4, n5, 0.01);
        }

        public void Connect(Neuron source, Neuron target, double w)
        {
            var synapse = new Synapse(source, target, w, 0);
            target.InputSynapses.Add(synapse);
        }

        public void Update(double dt, IReadOnlyDictionary<Neuron, double> additionalCurrent)
        {
            var inputs = Neurons.ToDictionary(neuron => neuron,
                neuron => neuron.InputCurrent(T, additionalCurrent.GetValueOrDefault(neuron, 0)));

            foreach (var neuron in Neurons)
            {
                double inputCurrent = inputs[neuron];

                neuron.Update(T, dt, inputCurrent);
            }

            T += dt;
        }

        public void PrintNet(IReadOnlyDictionary<Neuron, double> additionalCurrent)
        {
            Console.WriteLine("I");
            foreach (var neuron in Neurons)
            {
                F(neuron.InputCurrent(T, additionalCurrent[neuron]));
            }

            Console.WriteLine();
            Console.WriteLine("U+65");
            foreach (var neuron in Neurons)
            {
                F(neuron.U + 65);
            }

            Console.WriteLine();
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

        public IEnumerable<Synapse> Synapses
        {
            get
            {
                foreach (var neuron in Neurons)
                {
                    foreach (var synapse in neuron.InputSynapses)
                    {
                        yield return synapse;
                    }
                }
            }
        }
    }
}
