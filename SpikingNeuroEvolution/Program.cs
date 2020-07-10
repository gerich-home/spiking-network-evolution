using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using System.Threading;
using ScottPlot;
using static System.Math;

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

    class Chromosome
    {
        public readonly ImmutableHashSet<EdgeGene> EdgeGenes;
        public readonly int Nodes;

        public Chromosome(IEnumerable<EdgeGene> edgeGenes, int nodeGenes)
            : this(edgeGenes.ToImmutableHashSet(), nodeGenes)
        {
        }

        public Chromosome(ImmutableHashSet<EdgeGene> edgeGenes, int nodes)
        {
            EdgeGenes = edgeGenes;
            Nodes = nodes;
        }

        public Chromosome MutateAddNode(Func<ImmutableHashSet<EdgeGene>, EdgeGene> chooseGene)
        {
            var gene = chooseGene(EdgeGenes);
            var disabledGene = gene.Disable();
            var g1 = new EdgeGene(new GeneType(disabledGene.GeneType.From, Nodes), disabledGene.Weight, true);
            var g2 = new EdgeGene(new GeneType(Nodes, disabledGene.GeneType.To), disabledGene.Weight, true);

            var newEdgeGenes = EdgeGenes.Remove(gene).Union(new[]
            {
                disabledGene,
                g1,
                g2
            });

            return new Chromosome(newEdgeGenes, Nodes + 1);
        }

        public Chromosome MutateChangeWeight(Func<ImmutableHashSet<EdgeGene>, (EdgeGene, double)> chooseGeneAndWeight)
        {
            var (gene, newWeight) = chooseGeneAndWeight(EdgeGenes);

            var newEdgeGenes = EdgeGenes
                .Remove(gene)
                .Add(gene.ChangeWeight(newWeight));

            return new Chromosome(newEdgeGenes, Nodes);
        }

        public Chromosome MutateChangeEnabled(Func<ImmutableHashSet<EdgeGene>, (EdgeGene, bool)> chooseGeneAndEnabled)
        {
            var (gene, newIsEnabled) = chooseGeneAndEnabled(EdgeGenes);

            var newEdgeGenes = EdgeGenes
                .Remove(gene)
                .Add(gene.ChangeEnabled(newIsEnabled));

            return new Chromosome(newEdgeGenes, Nodes);
        }

        public static Chromosome Crossover(Chromosome chromosomeA, Chromosome chromosomeB,
            Func<EdgeGene, EdgeGene, EdgeGene> crossoverMatchingGene)
        {
            var edgeGenesA = EdgeGenes(chromosomeA);
            var edgeGenesB = EdgeGenes(chromosomeB);

            var keysA = edgeGenesA.Keys.ToImmutableHashSet();
            var keysB = edgeGenesB.Keys.ToImmutableHashSet();

            var disjointGenesA = keysA.Except(keysB).Select(key => edgeGenesA[key]);
            var disjointGenesB = keysB.Except(keysA).Select(key => edgeGenesB[key]);
            var crossedGenes = keysA.Intersect(keysB)
                .Select(key => crossoverMatchingGene(edgeGenesA[key], edgeGenesB[key]));

            return new Chromosome(disjointGenesA
                    .Concat(disjointGenesB)
                    .Concat(crossedGenes),
                Max(chromosomeA.Nodes, chromosomeB.Nodes));

            ImmutableDictionary<GeneType, EdgeGene> EdgeGenes(Chromosome chromosome)
            {
                return chromosome.EdgeGenes.ToImmutableDictionary(gene => gene.GeneType, gene => gene);
            }
        }
    }

    class EvaluatedChromosome
    {
        public Chromosome Chromosome { get; }
        public double Fitness { get; }

        public EvaluatedChromosome(Chromosome chromosome, double fitness)
        {
            Chromosome = chromosome;
            Fitness = fitness;
        }
    }

    struct GeneType
    {
        public readonly int From;
        public readonly int To;

        public GeneType(int @from, int to)
        {
            From = @from;
            To = to;
        }
    }

    class EdgeGene
    {
        public readonly double Weight;
        public readonly bool IsEnabled;
        public readonly GeneType GeneType;

        public EdgeGene(GeneType geneType, double weight, bool isEnabled)
        {
            GeneType = geneType;
            Weight = weight;
            IsEnabled = isEnabled;
        }

        public EdgeGene Disable()
        {
            return ChangeEnabled(false);
        }

        public EdgeGene Enable()
        {
            return ChangeEnabled(true);
        }

        public EdgeGene ChangeEnabled(bool newIsEnabled)
        {
            return IsEnabled == newIsEnabled
                ? this
                : new EdgeGene(GeneType, Weight, newIsEnabled);
        }

        public EdgeGene ChangeWeight(double newWeight)
        {
            return Weight != newWeight
                ? new EdgeGene(GeneType, newWeight, IsEnabled)
                : this;
        }
    }

    class CPNN
    {
        private readonly Chromosome _chromosome;

        public CPNN(Chromosome chromosome)
        {
            _chromosome = chromosome;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //TestNeat();
            TestSNN();
        }

        private static void TestNeat()
        {
            var chromosomes = Enumerable.Range(0, 100)
                .Select(p => new Chromosome(new[]
                    {
                        new EdgeGene(new GeneType(0, 2), 1, true),
                        new EdgeGene(new GeneType(1, 2), 1, true)
                    }, 3)
                )
                .ToList();

            var list = chromosomes.Select(chromosome => Evaluate(chromosome, new[] {10.0, 20.0}, 1, 5, 100))
                .ToList();

            foreach (var d in list)
            {
                Console.WriteLine(d.Sum());
            }
        }


        public static IReadOnlyList<int> Evaluate(Chromosome chromosome, IReadOnlyList<double> input, int outSize, double time, int iterations)
        {
            var neuralNetwork = BuildNeuralNetwork(chromosome);

            neuralNetwork
                .Update(iterations, input.Select((d, i) => (d, i))
                    .ToDictionary(t => neuralNetwork.Neurons[t.i], t => t.d));

            return neuralNetwork.Neurons
                .Skip(input.Count)
                .Take(outSize)
                .Select(neuron => neuron.Spikes.Count)
                .ToList();
        }

        private static NeuralNet BuildNeuralNetwork(Chromosome chromosome)
        {
            var neurons = chromosome.EdgeGenes
                .SelectMany(gene => new[] { gene.GeneType.From, gene.GeneType.To })
                .Distinct()
                .OrderBy(nodeIndex => nodeIndex)
                .Select(nodeIndex => new Neuron())
                .ToList();

            var neuralNetwork = new NeuralNet(neurons);

            foreach (var gene in chromosome.EdgeGenes.Where(gene => gene.IsEnabled))
            {
                neuralNetwork.Connect(neurons[gene.GeneType.From], neurons[gene.GeneType.To], gene.Weight);
            }

            return neuralNetwork;
        }

        private static void TestSNN()
        {
            var nn = new NeuralNet();
            nn.Initialize();

            var additionalCurrent = nn.Neurons.ToDictionary(neuron => neuron, neuron => 0.0);
            //additionalCurrent[nn.Neurons[1]] = 19;

            double t = 50;
            double dt = 0.005;
            int n = (int)(t / dt);

            var u = new double[nn.Neurons.Count][];
            var u2 = new double[nn.Neurons.Count][];
            var ss = new double[nn.Neurons.Count][];
            for (int k = 0; k < nn.Neurons.Count; k++)
            {
                u[k] = new double[n];
                u2[k] = new double[n];
                ss[k] = new double[n];
            }

            
            for (int j = 0; j < n; j++)
            {
                additionalCurrent[nn.Neurons[0]] = 15;
                additionalCurrent[nn.Neurons[1]] = 10;

                //}
                //while (true)
                //{

                //Console.Clear();
                //nn.PrintNet(additionalCurrent);

                for (int k = 0; k < nn.Neurons.Count; k++)
                {
                    u[k][j] = nn.Neurons[k].U;
                    u2[k][j] = nn.Neurons[k].U2;
                    ss[k][j] = nn.Neurons[k].SynapticStimulation(nn.T);
                }
                //Thread.Sleep(10);
                nn.Update(dt, additionalCurrent);
            }

            for (int k = 0; k < nn.Neurons.Count; k++)
            {
                var plot = new Plot();
                plot.PlotSignal(u[k], 1 / dt);
                plot.PlotSignal(u2[k], 1 / dt, lineStyle: LineStyle.Dash);
                plot.PlotSignal(ss[k], 1 / dt, lineStyle: LineStyle.Dot);
                foreach (var spike in nn.Neurons[k].Spikes)
                {
                    plot.PlotVLine(spike, Color.Red);
                }

                plot.SaveFig($"n{k}.png");
            }
        }
    }
}
