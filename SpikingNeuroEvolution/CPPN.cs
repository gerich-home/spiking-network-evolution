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
        public readonly ImmutableArray<NodeGeneType> InputGenes;
        public readonly ImmutableArray<NodeGeneType> OutputGenes;

        public CPPN(Chromosome chromosome, NodeGeneType[] inputGenes, NodeGeneType[] outputGenes)
        {
            Chromosome = chromosome;
            InputGenes = inputGenes.ToImmutableArray();
            OutputGenes = outputGenes.ToImmutableArray();
        }

        public double[] Calculate(double[] inputValues)
        {
            var edges = Chromosome.EdgeGenes
                .GroupBy(pair => pair.Key.From)
                .ToImmutableDictionary(g => g.Key, g => g.Select(pair => pair.Value).ToImmutableList());

            var nodeInput = Chromosome.NodeGenes
                .ToDictionary(pair => pair.Value.GeneType, _ => 0.0);
                
            var nodeOutput = Chromosome.NodeGenes
                .ToDictionary(pair => pair.Value.GeneType, _ => 0.0);

            var visitedNodes = new HashSet<NodeGeneType>();
            
            var nodesAddedToQueue = InputGenes.ToHashSet();
            var nodesQueue = new Queue<NodeGeneType>(InputGenes);
            
            InputGenes.Each((nodeGeneType, index) => nodeInput[nodeGeneType] = inputValues[index]);

            while (nodesQueue.Count > 0)
            {
                var nodeGeneType = nodesQueue.Dequeue();
                var nodeGene = Chromosome.NodeGenes[nodeGeneType];
                var value = NodeFunc(nodeGeneType)(nodeInput[nodeGeneType]);
                nodeOutput[nodeGeneType] = value;

                if (edges.TryGetValue(nodeGeneType, out var outEdges))
                {
                    foreach(var edge in outEdges)
                    {
                        var to = edge.GeneType.To;
                        nodeInput[to] += value * edge.Weight;
                        if (visitedNodes.Contains(to)) {
                            throw new InvalidOperationException("Loop in network");
                        }

                        if (!nodesAddedToQueue.Contains(to)) {
                            nodesQueue.Enqueue(to);
                            nodesAddedToQueue.Add(to);
                        }
                    }
                }

                visitedNodes.Add(nodeGeneType);
            }

            return OutputGenes.Select(gene => nodeOutput[gene]).ToArray();
        }

        Func<double, double> NodeFunc(NodeGeneType nodeGeneType)
        {
            switch(nodeGeneType.FunctionType) {
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
