using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    record class Species(ImmutableHashSet<Chromosome> Chromosomes)
    {
        public int Size => Chromosomes.Count;

        public override string ToString() => $"Species: {{{Size}}}";

        public double AverageFitness(EvaluatedPopulation evaluation) => Chromosomes.Average(evaluation.Fitness);
    }
}
