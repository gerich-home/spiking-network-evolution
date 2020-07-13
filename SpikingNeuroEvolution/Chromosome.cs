using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class Chromosome
    {
        public readonly ImmutableHashSet<NodeGene> NodeGenes;
        public readonly ImmutableDictionary<EdgeGeneType, EdgeGene> EdgeGenes;
        
        public Chromosome(ImmutableHashSet<NodeGene> nodeGenes, ImmutableDictionary<EdgeGeneType, EdgeGene> edgeGenes)
        {
            CheckValid(nodeGenes, edgeGenes);
            NodeGenes = nodeGenes;
            EdgeGenes = edgeGenes;
        }

        [Conditional("DEBUG")]
        private void CheckValid(ImmutableHashSet<NodeGene> nodeGenes, ImmutableDictionary<EdgeGeneType, EdgeGene> edgeGenes)
        {
            Debug.Assert(edgeGenes.Keys.All(edgeGeneType => nodeGenes.Contains(edgeGeneType.From)));
            Debug.Assert(edgeGenes.Keys.All(edgeGeneType => nodeGenes.Contains(edgeGeneType.To)));
        }

        // public Chromosome MutateAddNode(EdgeGeneType edgeGeneType)
        // {
        //     var gene = EdgeGenes[edgeGeneType];
        //     var disabledGene = gene.Disable();
        //     var newNodeGene = new NodeGene(FunctionType.Identity);
        //     var g1 = new EdgeGene(new EdgeGeneType(disabledGene.GeneType.From, newNodeGene), disabledGene.Weight, true);
        //     var g2 = new EdgeGene(new EdgeGeneType(newNodeGene, disabledGene.GeneType.To), disabledGene.Weight, true);

        //     var newEdgeGenes = EdgeGenes.Remove(edgeGeneType).Add(g1);

        //     return new Chromosome(newEdgeGenes, Nodes + 1);
        // }

        // public Chromosome MutateChangeWeight(Func<ImmutableHashSet<EdgeGene>, (EdgeGene, double)> chooseGeneAndWeight)
        // {
        //     var (gene, newWeight) = chooseGeneAndWeight(EdgeGenes);

        //     var newEdgeGenes = EdgeGenes
        //         .Remove(gene)
        //         .Add(gene.ChangeWeight(newWeight));

        //     return new Chromosome(newEdgeGenes, Nodes);
        // }

        // public Chromosome MutateChangeEnabled(Func<ImmutableHashSet<EdgeGene>, (EdgeGene, bool)> chooseGeneAndEnabled)
        // {
        //     var (gene, newIsEnabled) = chooseGeneAndEnabled(EdgeGenes);

        //     var newEdgeGenes = EdgeGenes
        //         .Remove(gene)
        //         .Add(gene.ChangeEnabled(newIsEnabled));

        //     return new Chromosome(newEdgeGenes, Nodes);
        // }

        // public static Chromosome Crossover(Chromosome chromosomeA, Chromosome chromosomeB,
        //     Func<EdgeGene, EdgeGene, EdgeGene> crossoverMatchingGene)
        // {
        //     var edgeGenesA = EdgeGenes(chromosomeA);
        //     var edgeGenesB = EdgeGenes(chromosomeB);

        //     var keysA = edgeGenesA.Keys.ToImmutableHashSet();
        //     var keysB = edgeGenesB.Keys.ToImmutableHashSet();

        //     var disjointGenesA = keysA.Except(keysB).Select(key => edgeGenesA[key]);
        //     var disjointGenesB = keysB.Except(keysA).Select(key => edgeGenesB[key]);
        //     var crossedGenes = keysA.Intersect(keysB)
        //         .Select(key => crossoverMatchingGene(edgeGenesA[key], edgeGenesB[key]));

        //     return new Chromosome(disjointGenesA
        //             .Concat(disjointGenesB)
        //             .Concat(crossedGenes),
        //         Max(chromosomeA.Nodes, chromosomeB.Nodes));

        //     ImmutableDictionary<GeneType, EdgeGene> EdgeGenes(Chromosome chromosome)
        //     {
        //         return chromosome.EdgeGenes.ToImmutableDictionary(gene => gene.GeneType, gene => gene);
        //     }
        // }
    }
}
