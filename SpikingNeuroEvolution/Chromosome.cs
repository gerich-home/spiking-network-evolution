using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class Chromosome
    {
        public readonly ImmutableDictionary<EdgeGeneType, EdgeGene> EdgeGenes;
        public readonly ImmutableDictionary<NodeGeneType, NodeGene> NodeGenes;

        public Chromosome(ImmutableDictionary<NodeGeneType, NodeGene> nodeGenes, ImmutableDictionary<EdgeGeneType, EdgeGene> edgeGenes)
        {
            CheckValid(nodeGenes, edgeGenes);
            NodeGenes = nodeGenes;
            EdgeGenes = edgeGenes;
        }

        [Conditional("DEBUG")]
        private void CheckValid(ImmutableDictionary<NodeGeneType, NodeGene> nodeGenes, ImmutableDictionary<EdgeGeneType, EdgeGene> edgeGenes)
        {
            Debug.Assert(edgeGenes.Keys.All(edgeGeneType => nodeGenes.ContainsKey(edgeGeneType.From)));
            Debug.Assert(edgeGenes.Keys.All(edgeGeneType => nodeGenes.ContainsKey(edgeGeneType.To)));
        }
    }
}
