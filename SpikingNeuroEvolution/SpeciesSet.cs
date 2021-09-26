using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class SpeciesSet
    {
        private ImmutableDictionary<Chromosome, Species> ChromosomesToSpecies { get; }
        public ImmutableList<Species> AllSpecies { get; }

        private SpeciesSet(ImmutableDictionary<Chromosome, Species> chromosomesToSpecies)
        {
            ChromosomesToSpecies = chromosomesToSpecies;
            AllSpecies = ChromosomesToSpecies.Values.Distinct().ToImmutableList();
        }

        public Species GetSpecies(Chromosome chromosome) => ChromosomesToSpecies[chromosome];
        public int Count => AllSpecies.Count;

        public static SpeciesSet Build(IEnumerable<Chromosome> chromosomes, ChromosomeComparisonParams comparisonParams)
        {
            var builder = ImmutableHashSet.CreateBuilder<Species>();
    
            var species = new Dictionary<Chromosome, HashSet<Chromosome>>();

            foreach(var chromosome in chromosomes) 
            {
                var specieRepresentative = species.Keys
                    .FirstOrDefault(representative => Chromosome.Compare(representative, chromosome, comparisonParams) < comparisonParams.Threshold);
                
                var targetSpecies = specieRepresentative == null ?
                    new HashSet<Chromosome>{} :
                    species[specieRepresentative];

                targetSpecies.Add(chromosome);
                species[chromosome] = targetSpecies;
            }

            var createdSpecies = species.Values.Distinct()
                .ToDictionary(chromosomesInSpecies => chromosomesInSpecies, chromosomesInSpecies => new Species(chromosomesInSpecies.ToImmutableHashSet()));
            
            return new SpeciesSet(species.ToImmutableDictionary(pair => pair.Key, pair => createdSpecies[pair.Value]));
        }
    }
}
