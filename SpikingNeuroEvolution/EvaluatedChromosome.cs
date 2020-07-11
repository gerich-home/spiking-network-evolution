namespace SpikingNeuroEvolution
{
    /*
    class ChromosomeOps
    {
        public Chromosome MutateAddNode(Func<ImmutableHashSet<EdgeGene>, EdgeGene> chooseGene)
        {
            var gene = chooseGene(EdgeGenes);
            var disabledGene = gene.Disable();
            var g1 = new EdgeGene(new GeneType(disabledGene.GeneType.From, Nodes), disabledGene.Weight, true);
            var g2 = new EdgeGene(new GeneType(Nodes, disabledGene.GeneType.To), disabledGene.Weight, true);

            var newEdgeGenes = EdgeGenes.Remove(gene).Union(new[]
            {
                disabledGene,
                g1,
                g2
            });

            return new Chromosome(newEdgeGenes, Nodes + 1);
        }

        public Chromosome MutateChangeWeight(Func<ImmutableHashSet<EdgeGene>, (EdgeGene, double)> chooseGeneAndWeight)
        {
            var (gene, newWeight) = chooseGeneAndWeight(EdgeGenes);

            var newEdgeGenes = EdgeGenes
                .Remove(gene)
                .Add(gene.ChangeWeight(newWeight));

            return new Chromosome(newEdgeGenes, Nodes);
        }

        public Chromosome MutateChangeEnabled(Func<ImmutableHashSet<EdgeGene>, (EdgeGene, bool)> chooseGeneAndEnabled)
        {
            var (gene, newIsEnabled) = chooseGeneAndEnabled(EdgeGenes);

            var newEdgeGenes = EdgeGenes
                .Remove(gene)
                .Add(gene.ChangeEnabled(newIsEnabled));

            return new Chromosome(newEdgeGenes, Nodes);
        }

        public static Chromosome Crossover(Chromosome chromosomeA, Chromosome chromosomeB,
            Func<EdgeGene, EdgeGene, EdgeGene> crossoverMatchingGene)
        {
            var edgeGenesA = EdgeGenes(chromosomeA);
            var edgeGenesB = EdgeGenes(chromosomeB);

            var keysA = edgeGenesA.Keys.ToImmutableHashSet();
            var keysB = edgeGenesB.Keys.ToImmutableHashSet();

            var disjointGenesA = keysA.Except(keysB).Select(key => edgeGenesA[key]);
            var disjointGenesB = keysB.Except(keysA).Select(key => edgeGenesB[key]);
            var crossedGenes = keysA.Intersect(keysB)
                .Select(key => crossoverMatchingGene(edgeGenesA[key], edgeGenesB[key]));

            return new Chromosome(disjointGenesA
                    .Concat(disjointGenesB)
                    .Concat(crossedGenes),
                Max(chromosomeA.Nodes, chromosomeB.Nodes));

            ImmutableDictionary<GeneType, EdgeGene> EdgeGenes(Chromosome chromosome)
            {
                return chromosome.EdgeGenes.ToImmutableDictionary(gene => gene.GeneType, gene => gene);
            }
        }
    }
    */

    class EvaluatedChromosome
    {
        public Chromosome Chromosome { get; }
        public double Fitness { get; }

        public EvaluatedChromosome(Chromosome chromosome, double fitness)
        {
            Chromosome = chromosome;
            Fitness = fitness;
        }
    }
}
