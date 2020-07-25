using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class GeneticAlgorithm
    {
        public void Test()
        {
            var (seedChromosome, inputGenes, outputGenes) = CreateSeedChromosome(2, 1);

            var rnd = new Random();

            const int populationSize = 30;
            const int mutants = 10;
            const int children = 5;
            var population = CreateInitialPopulation(seedChromosome, populationSize).ToImmutableList();
            
            var goldEvaluator = CreateChromosomeEvaluator(new (double, double)[]{
                (1, 2),
                (10, -2),
                (2, 2),
                (0.5, 4),
                (100, 20),
            }.ToImmutableList());
            EvaluatedChromosome best;
            for (int i = 0; true; i++) {
                best = EvaluateWithEvaluator(population, goldEvaluator)
                    .OrderByDescending(e => e.Fitness).First();
                Console.WriteLine(best.Fitness);
                
                population = NextPopulation(Evaluate(population));
            }
                
            for (int i = 1; i < 10; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Console.WriteLine($"f({i}, {j}) -> {EvaluatePhenotype(best.Chromosome, i, j)} ({TargetFunction(i, j)})");
                }
            }

            Console.WriteLine("Done");

            Func<Chromosome, double> CreateRandomChromosomeEvaluator() =>
                CreateChromosomeEvaluator(
                    Enumerable.Range(0, 10)
                        .Select(i => (rnd.NextDouble() * 100 - 50, rnd.NextDouble() * 100 - 50))
                        .ToImmutableList()
                    );

            Func<Chromosome, double> CreateChromosomeEvaluator(
                ImmutableList<(double, double)> examples
            ) => chromose => EvaluateChromosome(chromose, examples);

            double EvaluateChromosome(Chromosome chromosome, ImmutableList<(double, double)> examples)
            {
                try
                {
                    var cppn = new CPPN(chromosome, inputGenes, outputGenes);

                    double fitness = 0;

                    foreach(var (a, b) in examples) {
                        var result = cppn.Calculate(ImmutableArray.Create(a, b))[0];
                        if (double.IsNaN(result)) {
                            return 0;
                        } else {
                            fitness += Math.Pow(result - TargetFunction(a, b), 2);
                        }
                    }

                    return fitness == 0 ? double.PositiveInfinity : 1 / fitness;
                }
                catch
                {
                    return 0;
                }
            }

            double EvaluatePhenotype(Chromosome chromosome, double x, double y)
            {
                var cppn = new CPPN(chromosome, inputGenes, outputGenes);

                return cppn.Calculate(ImmutableArray.Create(x, y))[0];
            }

            double TargetFunction(double x, double y)
            {
                return x * y;
            }

            ImmutableList<Chromosome> NextPopulation(ImmutableList<EvaluatedChromosome> evaluatedPopulation)
            {
                var validPopulation = evaluatedPopulation
                    .Where(ec => ec.Fitness > 0)
                    .ToImmutableList();
                
                return EvaluateBySpecies(validPopulation).OrderByDescending(ec => ec.Fitness).Take(populationSize - (mutants + children)).Select(ec => ec.Chromosome)
                    .Concat(GetRandomOrderPopulation(validPopulation).Take(mutants).Select(ec => Mutate(ec.Chromosome)))
                    .Concat(GetRandomOrderPopulation(validPopulation).Take(children).Zip(GetRandomOrderPopulation(validPopulation).Take(children)).Select(Crossover))
                    .ToImmutableList();
            }

            IEnumerable<EvaluatedChromosome> EvaluateBySpecies(IEnumerable<EvaluatedChromosome> evaluatedChromosomes)
            {
                var species = new Dictionary<Chromosome, HashSet<Chromosome>>();
                var chromosomeToSpecies = new Dictionary<Chromosome, HashSet<Chromosome>>();
                foreach(var ec in evaluatedChromosomes) 
                {
                    var specieRepresentative = species.Keys
                        .FirstOrDefault(y => Chromosome.Compare(y, ec.Chromosome, 3, 1) < 2);
                    
                    if (specieRepresentative == null)
                    {
                        var newSpecie = new HashSet<Chromosome>{ec.Chromosome};
                        species[ec.Chromosome] = newSpecie;
                        chromosomeToSpecies[ec.Chromosome] = newSpecie;
                    }
                    else
                    {
                        var specie = species[specieRepresentative];
                        specie.Add(ec.Chromosome);
                        chromosomeToSpecies[ec.Chromosome] = specie;
                    }
                }


                var sizes = species.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
                return evaluatedChromosomes
                    .Select(ec => new EvaluatedChromosome(ec.Chromosome, ec.Fitness / chromosomeToSpecies[ec.Chromosome].Count));
            }

            ImmutableList<EvaluatedChromosome> Evaluate(
                IEnumerable<Chromosome> chromosomes
            ) => EvaluateWithEvaluator(chromosomes, CreateRandomChromosomeEvaluator());

            ImmutableList<EvaluatedChromosome> EvaluateWithEvaluator(
                IEnumerable<Chromosome> chromosomes,
                Func<Chromosome, double> chromosomeEvaluator
            ) => chromosomes
                .Select(chromosome => new EvaluatedChromosome(chromosome, chromosomeEvaluator(chromosome)))
                .ToImmutableList();

            Chromosome Crossover((EvaluatedChromosome First, EvaluatedChromosome Second) pair)
            {
                return Chromosome.Crossover(pair.First.Chromosome, pair.Second.Chromosome,
                    (ea, eb) => new EdgeGene((ea.Weight + eb.Weight) / 2, ea.IsEnabled ^ eb.IsEnabled ? rnd.NextDouble() < 0.5 : ea.IsEnabled && ea.IsEnabled)
                );
            }

            IEnumerable<EvaluatedChromosome> GetRandomOrderPopulation(IEnumerable<EvaluatedChromosome> population)
            {
                return population
                    .Select(chromosome => new { chromosome, order = rnd.Next() })
                    .OrderBy(p => p.order)
                    .Select(p => p.chromosome);
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

                if (choice < 0.003)
                {
                    return chromosome.MutateAddNode(rnd.Next, ChooseNodeType(), ChooseAggregationType(), rnd.NextDouble() * 2 - 1, rnd.NextDouble() * 2 - 1);
                }

                if (choice < 0.1)
                {
                    return chromosome.MutateAddEdge(rnd.Next, rnd.NextDouble() * 2 - 1);
                }

                if (choice < 0.9)
                {
                    return chromosome.MutateChangeWeight(rnd.Next, rnd.NextDouble() * 2 - 1);
                }

                return chromosome.MutateChangeEnabled(rnd.Next);
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

        private static (Chromosome, ImmutableArray<NodeGene>, ImmutableArray<NodeGene>) CreateSeedChromosome(int inputGenesCount, int outputGenesCount)
        {
            var inputGenes = CreateNodeGenes(inputGenesCount);
            var outputGenes = CreateNodeGenes(outputGenesCount);

            var seedChromosome = Chromosome.Build((e, n) =>
            {
                n.UnionWith(inputGenes);
                n.UnionWith(outputGenes);
                inputGenes.SelectMany(i => outputGenes.Select(o => new EdgeGeneType(i, o)))
                    .Each(edgeGeneType => e[edgeGeneType] = new EdgeGene(1.0, true));
            });

            return (seedChromosome, inputGenes, outputGenes);
        }

        private static ImmutableArray<NodeGene> CreateNodeGenes(int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => new NodeGene(FunctionType.Identity, AggregationType.Sum))
                .ToImmutableArray();
        }
    }
}
