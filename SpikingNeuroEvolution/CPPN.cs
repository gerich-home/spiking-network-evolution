using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static System.Math;

namespace SpikingNeuroEvolution
{
    record class CPPN(Chromosome Chromosome, ImmutableArray<NodeGeneType> InputGenes, ImmutableArray<NodeGeneType> OutputGenes)
    {
        public void Validate()
        {
            if (InputGenes.Any(gene => Chromosome.NodeGenes[gene].NodeType != NodeType.Input))
            {
                throw new ArgumentException("Non-input gene passed in input genes");
            }
            if (OutputGenes.Any(gene => Chromosome.NodeGenes[gene].NodeType != NodeType.Output))
            {
                throw new ArgumentException("Non-output gene passed in output genes");
            }
        }

        public ImmutableArray<double> Calculate(ImmutableArray<double> inputValues)
        {
            var enabledEdges = Chromosome.EdgeGenes
                            .Where(edgeGene => edgeGene.Value.IsEnabled)
                            .ToImmutableArray();
            var edgesByFrom = enabledEdges
                .GroupBy(pair => pair.Key.From)
                .ToImmutableDictionary(g => g.Key, g => g.Select(pair => pair.Key.To).ToImmutableArray());

            var edgesByTo = enabledEdges
                .GroupBy(pair => pair.Key.To)
                .ToImmutableDictionary(g => g.Key, g => g.Select(pair => pair.Key.From).ToImmutableArray());

            var dependenciesCount = Chromosome.NodeGenes.Keys
                .ToDictionary(geneType => geneType, dependenciesCount => 0.0);
            enabledEdges.Each(edgeGene => dependenciesCount[edgeGene.Key.To]++);

            var nodeInput = InputGenes
                .Zip(inputValues)
                .ToImmutableDictionary(pair => pair.First, pair => pair.Second);

            var nodeOutput = Chromosome.NodeGenes.Keys
                .ToDictionary(geneType => geneType, _ => 0.0);

            var nodesToVisit = new Queue<NodeGeneType>(dependenciesCount
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key));

            var visitedNodes = new HashSet<NodeGeneType>();

            while (nodesToVisit.Count > 0)
            {
                var nodeGeneType = nodesToVisit.Dequeue();
                var nodeGene = Chromosome.NodeGenes[nodeGeneType];
                visitedNodes.Add(nodeGeneType);

                double input = nodeInput.TryGetValue(nodeGeneType, out var externalInput) ?
                    externalInput :
                    0.0;
                
                double incomingTotal = edgesByTo.TryGetValue(nodeGeneType, out var inEdges) ?
                    nodeGene.AggregationType.AggregateFunction(
                        inEdges.Select(
                            fromNode => nodeOutput[fromNode] *
                                Chromosome.EdgeGenes[new EdgeGeneType(fromNode, nodeGeneType)].Weight
                        )
                    ) :
                    0.0;

                nodeOutput[nodeGeneType] = Chromosome.NodeGenes[nodeGeneType].FunctionType.NodeFunc(input + incomingTotal);

                if (!edgesByFrom.TryGetValue(nodeGeneType, out var outEdges))
                {
                    continue;
                }

                foreach (var to in outEdges)
                {
                    dependenciesCount[to]--;

                    if (dependenciesCount[to] == 0)
                    {
                        nodesToVisit.Enqueue(to);
                    }
                }
            }

            if (visitedNodes.Count < Chromosome.NodeGenes.Count) {
                throw new LoopInCPPNException();
            }

            return OutputGenes.Select(gene => nodeOutput[gene]).ToImmutableArray();
        }
    }
}
