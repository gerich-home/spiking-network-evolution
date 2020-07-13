using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static System.Math;

namespace SpikingNeuroEvolution
{
    class CPPN
    {
        public readonly Chromosome Chromosome;
        public readonly ImmutableArray<NodeGene> InputGenes;
        public readonly ImmutableArray<NodeGene> OutputGenes;

        public CPPN(Chromosome chromosome, NodeGene[] inputGenes, NodeGene[] outputGenes)
        {
            Chromosome = chromosome;
            InputGenes = inputGenes.ToImmutableArray();
            OutputGenes = outputGenes.ToImmutableArray();
        }

        public double[] Calculate(double[] inputValues)
        {
            var edges = Chromosome.EdgeGenes
                .GroupBy(pair => pair.Key.From)
                .ToImmutableDictionary(g => g.Key, g => g.Select(pair => new {
                    pair.Key.To,
                    pair.Value
                }).ToImmutableList());

            var nodeInput = Chromosome.NodeGenes
                .ToDictionary(geneType => geneType, _ => 0.0);
                
            var nodeOutput = Chromosome.NodeGenes
                .ToDictionary(geneType => geneType, _ => 0.0);

            var visitedNodes = new HashSet<NodeGene>();
            
            var nodesAddedToQueue = InputGenes.ToHashSet();
            var nodesQueue = new Queue<NodeGene>(InputGenes);
            
            InputGenes.Each((nodeGene, index) => nodeInput[nodeGene] = inputValues[index]);

            while (nodesQueue.Count > 0)
            {
                var nodeGene = nodesQueue.Dequeue();
                var value = NodeFunc(nodeGene)(nodeInput[nodeGene]);
                nodeOutput[nodeGene] = value;

                if (edges.TryGetValue(nodeGene, out var outEdges))
                {
                    foreach(var edge in outEdges)
                    {
                        var to = edge.To;
                        nodeInput[to] += value * edge.Value.Weight;
                        if (visitedNodes.Contains(to)) {
                            throw new InvalidOperationException("Loop in network");
                        }

                        if (!nodesAddedToQueue.Contains(to)) {
                            nodesQueue.Enqueue(to);
                            nodesAddedToQueue.Add(to);
                        }
                    }
                }

                visitedNodes.Add(nodeGene);
            }

            return OutputGenes.Select(gene => nodeOutput[gene]).ToArray();
        }

        Func<double, double> NodeFunc(NodeGene nodeGene)
        {
            switch(nodeGene.FunctionType) {
                case FunctionType.Identity:
                    return x => x;
                case FunctionType.Heaviside:
                    return x => x > 0 ? 1 : 0;
                case FunctionType.Sin:
                    return Sin;
                case FunctionType.Exponent:
                    return Exp;
                case FunctionType.Log:
                    return Log;
            }

            throw new ArgumentException();
        }
    }
}
