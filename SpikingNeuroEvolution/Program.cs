using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Linq;
using ScottPlot;

namespace SpikingNeuroEvolution
{

    class Program
    {
        static void Main(string[] args)
        {
            //TestCPPN();
            //TestNeat();
            //TestSNN();
            new GeneticAlgorithm().Test();
        }

        private static void TestCPPN()
        {
            var n1 = new NodeGeneType(NodeGeneType.RandomInnovationId());
            var n2 = new NodeGeneType(NodeGeneType.RandomInnovationId());
            var n3 = new NodeGeneType(NodeGeneType.RandomInnovationId());
            var n4 = new NodeGeneType(NodeGeneType.RandomInnovationId());

            var chromosome = Chromosome.Build((e, n) => {
                n.Add(n1, new NodeGene(FunctionType.Identity, AggregationType.Sum, NodeType.Input));
                n.Add(n2, new NodeGene(FunctionType.Identity, AggregationType.Sum, NodeType.Input));
                n.Add(n3, new NodeGene(FunctionType.Sin, AggregationType.Sum, NodeType.Inner));
                n.Add(n4, new NodeGene(FunctionType.Identity, AggregationType.Sum, NodeType.Output));

                e.Add(new EdgeGeneType(n1, n3), new EdgeGene(1, true));
                e.Add(new EdgeGeneType(n2, n3), new EdgeGene(1, true));
                e.Add(new EdgeGeneType(n1, n4), new EdgeGene(1, true));
                e.Add(new EdgeGeneType(n3, n4), new EdgeGene(1, true));
            });

            var a = chromosome
                .MutateAddNode(n => n / 2, new NodeGene(FunctionType.Log, AggregationType.Sum, NodeType.Inner), 1, 1)
                .MutateAddNode(n => n / 3, new NodeGene(FunctionType.Log, AggregationType.Sum, NodeType.Inner), 1, 1)
                .MutateChangeEnabled(n => n / 4)
                .MutateChangeWeight(n => n / 3, 1);

            var b = chromosome
                .MutateAddNode(n => n / 2, new NodeGene(FunctionType.Exponent, AggregationType.Sum, NodeType.Inner), 1, 1)
                .MutateAddNode(n => n / 2, new NodeGene(FunctionType.Heaviside, AggregationType.Sum, NodeType.Inner), 1, 1);
            
            var c = Chromosome.Crossover(a, b,
                (na, nb) => new NodeGene(na.FunctionType, na.AggregationType, na.NodeType),
                (ea, eb) => new EdgeGene((ea.Weight + eb.Weight)/ 2, true)
            );

            var cppn = new CPPN(a, ImmutableArray.Create(n1, n2), ImmutableArray.Create(n4));

            Console.WriteLine(cppn.Calculate(ImmutableArray.Create(1.0, 1.0))[0]);
            // n4 = n3 + n1
            // n3 = Sin(n1 + n2)
            // 
        }

        private static void TestNeat()
        {
            /*
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
            */
        }


        /*
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
        */

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
