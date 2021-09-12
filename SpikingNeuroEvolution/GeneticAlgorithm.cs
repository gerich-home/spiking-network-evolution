using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class Species
    {
        public ImmutableHashSet<Chromosome> Chromosomes { get; }
        public int Size => Chromosomes.Count;

        public double AverageFitness(EvaluatedPopulation evaluation) => Chromosomes.Average(evaluation.Fitness);

        public Species(ImmutableHashSet<Chromosome> chromosomes)
        {
            Chromosomes = chromosomes;
        }
    }

    class SpeciesSet
    {
        private ImmutableDictionary<Chromosome, Species> ChromosomesToSpecies { get; }

        private SpeciesSet(ImmutableDictionary<Chromosome, Species> chromosomesToSpecies)
        {
            ChromosomesToSpecies = chromosomesToSpecies;
        }

        public Species GetSpecies(Chromosome chromosome) => ChromosomesToSpecies[chromosome];
        public IEnumerable<Species> AllSpecies => ChromosomesToSpecies.Values;

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

            var createdSpecies = species.Values
                .ToDictionary(chromosomesInSpecies => chromosomesInSpecies, chromosomesInSpecies => new Species(chromosomesInSpecies.ToImmutableHashSet()));
            
            return new SpeciesSet(species.ToImmutableDictionary(pair => pair.Key, pair => createdSpecies[pair.Value]));
        }
    }

    class ChromosomeComparisonParams
    {
        public double NodeWeight { get; }
        public double EdgeWeight { get; }
        public double Threshold { get; }

        public ChromosomeComparisonParams(double nodeWeight, double edgeWeight, double threshold)
        {
            NodeWeight = nodeWeight;
            EdgeWeight = edgeWeight;
            Threshold = threshold;
        }
    }
    
    class GeneticAlgorithm
    {
        public void Test()
        {
            var (seedChromosome, inputGenes, outputGenes) = CreateSeedChromosome(3, 1);

            var rnd = new Random();

            const int populationSize = 30;
            const int mutants = 10;
            const int children = 5;
            var population = EvaluatePopulation(CreateInitialPopulation(seedChromosome, populationSize));

            var goldExamples = new (double, double)[]{
                (1, 2),
                (10, -2),
                (-10, -2),
                (10, 2),
                (-10, 2),
                (2, 2),
                (0.5, 4),
                (100, 20),
            }.ToImmutableArray();
            var goldEvaluator = CreateChromosomeEvaluator(goldExamples);
            Chromosome best;
            for (int i = 0; true; i++) {
                var goldEvaluation = new EvaluatedPopulation(population.Chromosomes, goldEvaluator);

                best = goldEvaluation.Best;
                Console.WriteLine($"nodes={best.NodeGenes.Count - (inputGenes.Length + outputGenes.Length)} edges={best.EdgeGenes.Count(e => e.Value.IsEnabled)} -> fitness: {1 / goldEvaluation.Fitness(best)}");

                foreach(var example in goldExamples)
                {
                    var (x, y) = example;
                    Console.WriteLine($"f({x}, {y}) -> {EvaluatePhenotype(best, example)}");
                }
                
                population = NextPopulation(population);
            }
                
            for (int i = 1; i < 10; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Console.WriteLine($"f({i}, {j}) -> {EvaluatePhenotype(best, (i, j))} ({TargetFunction(i, j)})");
                }
            }

            Console.WriteLine("Done");

            Func<Chromosome, double> CreateRandomChromosomeEvaluator() =>
                CreateChromosomeEvaluator(
                    Enumerable.Range(0, 20)
                        .Select(i => (rnd.NextDouble() * 100 - 50, rnd.NextDouble() * 100 - 50))
                        .ToImmutableArray()
                    );

            Func<Chromosome, double> CreateChromosomeEvaluator(
                ImmutableArray<(double, double)> examples
            ) => chromosome => EvaluateChromosome(chromosome, examples);

            double EvaluateChromosome(Chromosome chromosome, IEnumerable<(double, double)> examples)
            {
                try
                {
                    var cppn = new CPPN(chromosome, inputGenes, outputGenes);

                    double worst = 0;

                    foreach(var (a, b) in examples) {
                        var result = cppn.Calculate(ImmutableArray.Create(1, a, b))[0];
                        if (double.IsNaN(result)) {
                            return double.NaN;
                        } else if(double.IsInfinity(result)) {
                            return double.NegativeInfinity;
                        } else {
                            worst = Math.Max(worst, Math.Abs(result - TargetFunction(a, b)));
                        }
                    }

                    if (worst == 0) {
                        return double.PositiveInfinity;
                    }

                    return 1 / worst;
                }
                catch
                {
                    return double.NaN;
                }
            }

            double EvaluatePhenotype(Chromosome chromosome, (double, double) example)
            {
                var cppn = new CPPN(chromosome, inputGenes, outputGenes);
                var (x, y) = example;

                return cppn.Calculate(ImmutableArray.Create(1, x, y))[0];
            }

            double TargetFunction(double x, double y)
            {
                return x * y + Math.Sin(x * y);
            }

            EvaluatedPopulation NextPopulation(EvaluatedPopulation evaluatedPopulation)
            {
                var speciesSet = SpeciesSet.Build(evaluatedPopulation.AliveChromosomes, new ChromosomeComparisonParams(3, 1, 2));

                var sizes = speciesSet.AllSpecies.ToDictionary(species => species, species => species.Size);
                var speciesFitnesses = speciesSet.AllSpecies.ToDictionary(species => species, species => species.AverageFitness(evaluatedPopulation));
                var totalPopulationSize = evaluatedPopulation.Size;
                var totalFitness = speciesFitnesses.Sum(p => p.Value);
                var fitnessCost = totalPopulationSize / totalFitness;

                var chromosomes = speciesSet.AllSpecies
                    .SelectMany(species =>
                    {
                        var allocatedSize = (int)(speciesFitnesses[species] * fitnessCost);

                        return species.Chromosomes
                            .Select(Mutate)
                            .Take(mutants)
                            .Take(allocatedSize);
                    });
                return EvaluatePopulation(chromosomes);
            }

            IEnumerable<Chromosome> SelectChromosomes(
                EvaluatedPopulation evaluatedPopulation
            ) => evaluatedPopulation.Chromosomes;

            EvaluatedPopulation EvaluatePopulation(
                IEnumerable<Chromosome> chromosomes
            ) => new EvaluatedPopulation(chromosomes, CreateRandomChromosomeEvaluator());

            Chromosome Crossover((Chromosome First, Chromosome Second) pair)
            {
                return Chromosome.Crossover(pair.First, pair.Second,
                    (na, nb) => new NodeGene(rnd.NextDouble() < 0.5 ? na.FunctionType : nb.FunctionType, rnd.NextDouble() < 0.5 ? na.AggregationType : nb.AggregationType, na.NodeType),
                    (ea, eb) => new EdgeGene((ea.Weight + eb.Weight) / 2, ea.IsEnabled ^ eb.IsEnabled ? rnd.NextDouble() < 0.5 : ea.IsEnabled && ea.IsEnabled)
                );
            }

            IEnumerable<Chromosome> CreateInitialPopulation(Chromosome seedChromosome, int populationSize)
            {
                yield return seedChromosome;

                foreach (var i in Enumerable.Range(0, populationSize - 1))
                {
                    yield return Mutate(seedChromosome);
                }
            }

            Chromosome Mutate(Chromosome chromosome)
            {
                var choice = rnd.NextDouble();

                if (choice < 0.01)
                {
                    return chromosome.MutateAddNode(rnd.Next, RandomNode());
                }

                if (choice < 0.01)
                {
                    return chromosome.MutateChangeNode(rnd.Next, n => RandomNode());
                }

                if (choice < 0.2)
                {
                    return chromosome.MutateAddEdge(rnd.Next, rnd.NextGaussian(0, 2));
                }

                if (choice < 0.2)
                {
                    return chromosome.MutateCollapseNode(rnd.Next);
                }

                if (choice < 0.35)
                {
                    return chromosome.MutateDeleteNode(rnd.Next);
                }

                if (choice < 0.9)
                {
                    return chromosome.MutateChangeWeight(rnd.Next, rnd.NextGaussian(0, 10));
                }

                return chromosome.MutateChangeEnabled(rnd.Next);
            }

            NodeGene RandomNode()
            {
                return new NodeGene(ChooseNodeType(), ChooseAggregationType(), NodeType.Inner);
            }

            FunctionType ChooseNodeType()
            {
                var choice = rnd.NextDouble();

                if (choice < 0.05)
                {
                    return FunctionType.Sin;
                }

                if (choice < 0.1)
                {
                    return FunctionType.Log;
                }

                if (choice < 0.15)
                {
                    return FunctionType.Exponent;
                }

                if (choice < 0.2)
                {
                    return FunctionType.Heaviside;
                }

                if (choice < 0.25)
                {
                    return FunctionType.Sigmoid;
                }

                return FunctionType.Identity;
            }

            AggregationType ChooseAggregationType()
            {
                var choice = rnd.NextDouble();

                if (choice < 0.2)
                {
                    return AggregationType.Multiply;
                }

                if (choice < 0.25)
                {
                    return AggregationType.Avg;
                }

                if (choice < 0.3)
                {
                    return AggregationType.Max;
                }

                if (choice < 0.35)
                {
                    return AggregationType.MaxAbs;
                }

                if (choice < 0.4)
                {
                    return AggregationType.Min;
                }

                if (choice < 0.45)
                {
                    return AggregationType.MinAbs;
                }

                return AggregationType.Sum;
            }
        }

        private static (Chromosome, ImmutableArray<NodeGeneType>, ImmutableArray<NodeGeneType>) CreateSeedChromosome(int inputGenesCount, int outputGenesCount)
        {
            var inputGenes = CreateNodeGenes(inputGenesCount, "input");
            var outputGenes = CreateNodeGenes(outputGenesCount, "output");

            var seedChromosome = Chromosome.Build((e, n) =>
            {
                n.AddRange(inputGenes.Select(ng => KeyValuePair.Create(ng, new NodeGene(FunctionType.Identity, AggregationType.Sum, NodeType.Input))));
                n.AddRange(outputGenes.Select(ng => KeyValuePair.Create(ng, new NodeGene(FunctionType.Identity, AggregationType.Sum, NodeType.Output))));
            });

            return (seedChromosome, inputGenes, outputGenes);
        }

        private static ImmutableArray<NodeGeneType> CreateNodeGenes(int count, string baseValue)
        {
            return Enumerable.Range(0, count)
                .Select(i => new NodeGeneType(NodeGeneType.Hash(baseValue + i)))
                .ToImmutableArray();
        }
    }
}
