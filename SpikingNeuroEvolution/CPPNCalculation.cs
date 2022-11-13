using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    record class CPPNCalculation(Chromosome Chromosome, ImmutableArray<KeyValuePair<EdgeGeneType, EdgeGene>> EnabledEdges, ImmutableDictionary<NodeGeneType, ImmutableArray<NodeGeneType>> EdgesByFrom, ImmutableDictionary<NodeGeneType, ImmutableArray<NodeGeneType>> EdgesByTo, ImmutableDictionary<NodeGeneType, double> NodeInput)
    {
        public ImmutableDictionary<NodeGeneType, double> Calculate()
        {
            var dependenciesCount = BuildInitialDependenciesCount();

            var nodeOutput = Chromosome.NodeGenes.Keys
                .ToDictionary(geneType => geneType, _ => 0.0);

            var nodesToVisit = new Stack<NodeGeneType>(dependenciesCount
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key));

            var visitedNodes = new HashSet<NodeGeneType>(Chromosome.NodeGenes.Count);

            while (nodesToVisit.Count > 0)
            {
                var nodeGeneType = nodesToVisit.Pop();
                visitedNodes.Add(nodeGeneType);

                double externalInput = GetExternalInput(nodeGeneType);

                var incomingInputs = CalculateIncomingInputs(nodeOutput, nodeGeneType);

                nodeOutput[nodeGeneType] = Chromosome.NodeGenes[nodeGeneType].CalculateOutput(incomingInputs, externalInput);

                if (EdgesByFrom.TryGetValue(nodeGeneType, out var outEdges))
                {
                    foreach (var to in outEdges)
                    {
                        dependenciesCount[to]--;

                        if (dependenciesCount[to] == 0)
                        {
                            nodesToVisit.Push(to);
                        }
                    }
                }
            }

            if (visitedNodes.Count != Chromosome.NodeGenes.Count)
            {
                throw new LoopInCPPNException();
            }

            return nodeOutput.ToImmutableDictionary();
        }

        private double GetExternalInput(NodeGeneType nodeGeneType) =>
            NodeInput.TryGetValue(nodeGeneType, out var externalInput) ?
                externalInput :
                0.0;

        private ImmutableArray<double> CalculateIncomingInputs(Dictionary<NodeGeneType, double> nodeOutput, NodeGeneType nodeGeneType)
        {
            if (!EdgesByTo.TryGetValue(nodeGeneType, out var inEdges))
            {
                return ImmutableArray<double>.Empty;
            }
            
            return inEdges
                .Select(
                    fromNodeType => nodeOutput[fromNodeType] *
                        Chromosome.EdgeGenes[new EdgeGeneType(fromNodeType, nodeGeneType)].Weight
                )
                .ToImmutableArray();
        }

        private Dictionary<NodeGeneType, double> BuildInitialDependenciesCount()
        {
            var dependenciesCount = Chromosome.NodeGenes.Keys
                .ToDictionary(geneType => geneType, dependenciesCount => 0.0);
            EnabledEdges.Each(edgeGene => dependenciesCount[edgeGene.Key.To]++);
            return dependenciesCount;
        }
    }
}
