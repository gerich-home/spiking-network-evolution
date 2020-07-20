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

            const int populationSize = 300;
            const int mutants = 60;
            const int children = 50;
            var population = CreateInitialPopulation(seedChromosome, populationSize).ToImmutableHashSet();
            
            for(int i = 0; i < 40; i++) {
                population = NextPopulation(population);
            }

            var best = population.First();
            Console.WriteLine(EvaluateChromosome(best));

            for (int i = 1; i < 10; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Console.WriteLine($"f({i}, {j}) -> {EvaluatePhenotype(best, i, j)} ({TargetFunction(i, j)})");
                }
            }

            Console.WriteLine("Done");

            double EvaluateChromosome(Chromosome chromosome)
            {
                try
                {
                    var cppn = new CPPN(chromosome, inputGenes, outputGenes);

                    double fitness = 0;
                    for (int i = 1; i < 10; i++)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            var result = cppn.Calculate(ImmutableArray.Create((double)i, (double)j))[0];
                            if (double.IsNaN(result)) {
                                return double.PositiveInfinity;
                            } else {
                                fitness += Math.Pow(result - TargetFunction(i, j), 2);
                            }
                        }
                    }

                    return fitness;
                }
                catch
                {
                    return double.PositiveInfinity;
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

            ImmutableHashSet<Chromosome> NextPopulation(ImmutableHashSet<Chromosome> population)
            {
                var validChromosomes = population
                    .Select(chromosome => new { chromosome, fitness = EvaluateChromosome(chromosome) })
                    .Where(x => !double.IsPositiveInfinity(x.fitness))
                    .ToImmutableList();

                var evaluatedPopulation = validChromosomes
                    .OrderBy(p => p.fitness)
                    .Select(p => p.chromosome)
                    .ToImmutableList();

                var validPopulation = validChromosomes.Select(x => x.chromosome).ToImmutableList();
                
                return evaluatedPopulation.Take(populationSize - (mutants + children))
                    .Concat(GetRandomOrderPopulation(validPopulation).Take(mutants).Select(Mutate))
                    .Concat(GetRandomOrderPopulation(validPopulation).Take(children).Zip(GetRandomOrderPopulation(population).Take(children)).Select(Crossover))
                    .ToImmutableHashSet();
            }

            Chromosome Crossover((Chromosome First, Chromosome Second) pair)
            {
                return Chromosome.Crossover(pair.First, pair.Second,
                    (ea, eb) => new EdgeGene((ea.Weight + eb.Weight) / 2, ea.IsEnabled ^ eb.IsEnabled ? rnd.NextDouble() < 0.5 : ea.IsEnabled && ea.IsEnabled)
                );
            }

            IEnumerable<Chromosome> GetRandomOrderPopulation(IEnumerable<Chromosome> population)
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

                if (choice < 0.3)
                {
                    return chromosome.MutateAddEdge(rnd.Next, rnd.NextDouble() * 2 - 1);
                }

                if (choice < 0.5)
                {
                    return chromosome.MutateAddNode(rnd.Next, ChooseNodeType(), ChooseAggregationType(), rnd.NextDouble() * 2 - 1, rnd.NextDouble() * 2 - 1);
                }

                if (choice < 0.95)
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
