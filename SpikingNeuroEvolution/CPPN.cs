using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
            var enabledEdges = Chromosome.EnabledEdges;
            var edgesByFrom = GroupEdgesByFrom(enabledEdges);
            var edgesByTo = GroupEdgesByTo(enabledEdges);
            var nodeInput = BuildNodeInput(inputValues);

            var calculation = new CPPNCalculation(Chromosome, enabledEdges, edgesByFrom, edgesByTo, nodeInput);

            var nodeOutput = calculation.Calculate();

            return OutputGenes.Select(gene => nodeOutput[gene]).ToImmutableArray();
        }

        private static ImmutableDictionary<NodeGeneType, ImmutableArray<NodeGeneType>> GroupEdgesByTo(ImmutableArray<KeyValuePair<EdgeGeneType, EdgeGene>> enabledEdges) =>
            enabledEdges
                .GroupBy(pair => pair.Key.To)
                .ToImmutableDictionary(g => g.Key, g => g.Select(pair => pair.Key.From).ToImmutableArray());

        private static ImmutableDictionary<NodeGeneType, ImmutableArray<NodeGeneType>> GroupEdgesByFrom(ImmutableArray<KeyValuePair<EdgeGeneType, EdgeGene>> enabledEdges) =>
            enabledEdges
                .GroupBy(pair => pair.Key.From)
                .ToImmutableDictionary(g => g.Key, g => g.Select(pair => pair.Key.To).ToImmutableArray());

        private ImmutableDictionary<NodeGeneType, double> BuildNodeInput(ImmutableArray<double> inputValues) =>
            InputGenes
                .Zip(inputValues)
                .ToImmutableDictionary(pair => pair.First, pair => pair.Second);
    }
}
