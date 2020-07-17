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

        public CPPN(Chromosome chromosome, ImmutableArray<NodeGene> inputGenes, ImmutableArray<NodeGene> outputGenes)
        {
            Chromosome = chromosome;
            InputGenes = inputGenes;
            OutputGenes = outputGenes;
        }

        public ImmutableArray<double> Calculate(ImmutableArray<double> inputValues)
        {
            var enabledEdges = Chromosome.EdgeGenes
                            .Where(edgeGene => edgeGene.Value.IsEnabled)
                            .ToImmutableList();
            var edgesByFrom = enabledEdges
                .GroupBy(pair => pair.Key.From)
                .ToImmutableDictionary(g => g.Key, g => g.Select(pair => pair.Key.To).ToImmutableArray());

            var dependenciesCount = Chromosome.NodeGenes
                .ToDictionary(geneType => geneType, dependenciesCount => 0.0);
            enabledEdges.Each(edgeGene => dependenciesCount[edgeGene.Key.To]++);

            var nodeInput = Chromosome.NodeGenes
                .ToDictionary(geneType => geneType, _ => 0.0);
            InputGenes.Each((nodeGene, index) => nodeInput[nodeGene] = inputValues[index]);

            var nodeOutput = Chromosome.NodeGenes
                .ToDictionary(geneType => geneType, _ => 0.0);

            var nodesToVisit = new Queue<NodeGene>(dependenciesCount
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key));

            var visitedNodes = new HashSet<NodeGene>();

            while (nodesToVisit.Count > 0)
            {
                var nodeGene = nodesToVisit.Dequeue();
                visitedNodes.Add(nodeGene);

                var value = NodeFunc(nodeGene)(nodeInput[nodeGene]);
                nodeOutput[nodeGene] = value;

                if (!edgesByFrom.TryGetValue(nodeGene, out var outEdges))
                {
                    continue;
                }

                foreach (var to in outEdges)
                {
                    nodeInput[to] += value * Chromosome.EdgeGenes[new EdgeGeneType(nodeGene, to)].Weight;
                    dependenciesCount[to]--;

                    if (dependenciesCount[to] == 0)
                    {
                        nodesToVisit.Enqueue(to);
                    }
                }
            }

            if (visitedNodes.Count < Chromosome.NodeGenes.Count) {
                throw new Exception("Loop in CPPN");
            }

            return OutputGenes.Select(gene => nodeOutput[gene]).ToImmutableArray();
        }

        Func<double, double> NodeFunc(NodeGene nodeGene)
        {
            switch (nodeGene.FunctionType)
            {
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
