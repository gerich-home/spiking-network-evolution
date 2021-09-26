using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class Species
    {
        public ImmutableHashSet<Chromosome> Chromosomes { get; }
        public int Size => Chromosomes.Count;

        public override string ToString() => $"Species: {{{Size}}}";


        public double AverageFitness(EvaluatedPopulation evaluation) => Chromosomes.Average(evaluation.Fitness);

        public Species(ImmutableHashSet<Chromosome> chromosomes)
        {
            Chromosomes = chromosomes;
        }
    }
}
