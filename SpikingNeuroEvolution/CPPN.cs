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

        public CPPN(Chromosome chromosome, ImmutableArray<NodeGeneType> inputGenes, ImmutableArray<NodeGeneType> outputGenes)
        {
            Chromosome = chromosome;
            InputGenes = inputGenes;
            OutputGenes = outputGenes;

            if (inputGenes.Any(gene => chromosome.NodeGenes[gene].NodeType != NodeType.Input))
            {
                throw new ArgumentException("Non-input gene passed");
            }
            if (outputGenes.Any(gene => chromosome.NodeGenes[gene].NodeType != NodeType.Output))
            {
                throw new ArgumentException("Non-output gene passed");
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
                    Aggregate(
                        nodeGene.AggregationType,
                        inEdges.Select(
                            fromNode => nodeOutput[fromNode] *
                                Chromosome.EdgeGenes[new EdgeGeneType(fromNode, nodeGeneType)].Weight
                            )
                        ) :
                    0.0;

                nodeOutput[nodeGeneType] = NodeFunc(Chromosome.NodeGenes[nodeGeneType])(input + incomingTotal);

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
                throw new Exception("Loop in CPPN");
            }

            return OutputGenes.Select(gene => nodeOutput[gene]).ToImmutableArray();
        }

        private double Aggregate(AggregationType aggregationType, IEnumerable<double> inputs)
        {
            switch(aggregationType) 
            {
                case AggregationType.Sum:
                    return inputs.Sum();
                case AggregationType.Avg:
                    return inputs.Average();
                case AggregationType.Multiply:
                    return inputs.Aggregate(1.0, (x, y) => x * y);
                case AggregationType.Max:
                    return inputs.Max();
                case AggregationType.Min:
                    return inputs.Min();
                case AggregationType.MinAbs:
                    return inputs.Min();
                case AggregationType.MaxAbs:
                    return inputs.Max();
            }

            throw new ArgumentException();
        }

        Func<double, double> NodeFunc(NodeGene nodeGene)
        {
            switch (nodeGene.FunctionType)
            {
                case FunctionType.Identity:
                    return x => x;
                case FunctionType.Heaviside:
                    return x => x > 0 ? 1 : 0;
                case FunctionType.Sigmoid:
                    return x => 1 / (1 + Exp(-4.9 * x));
                case FunctionType.Sin:
                    return Sin;
                case FunctionType.Exponent:
                    return Exp;
                case FunctionType.Log:
                    return x => x <= 0 ? 0 : Log(x);
            }

            throw new ArgumentException();
        }
    }
}
