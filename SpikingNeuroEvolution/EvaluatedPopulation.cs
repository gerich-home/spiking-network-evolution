using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    record class EvaluatedPopulation
    {
        private IImmutableDictionary<Chromosome, double> FitnessesByChromosomes { get; }

        public EvaluatedPopulation(IEnumerable<Chromosome> chromosomes, Func<Chromosome, double> fitnessEvaluator)
        {
            FitnessesByChromosomes = chromosomes.ToImmutableDictionary(
                chromosome => chromosome,
                chromosome => fitnessEvaluator(chromosome)
            );
        }

        public IEnumerable<Chromosome> Chromosomes => FitnessesByChromosomes.Keys;
        public double Fitness(Chromosome chromosome) => FitnessesByChromosomes[chromosome];
        public IEnumerable<Chromosome> OrderedChromosomes => Chromosomes.OrderByDescending(Fitness);
        public IEnumerable<Chromosome> AliveChromosomes => Chromosomes.Where(chromosome => !double.IsNaN(Fitness(chromosome)));
        public Chromosome Best => OrderedChromosomes.First();
        public int Size => FitnessesByChromosomes.Count;
    }
}
