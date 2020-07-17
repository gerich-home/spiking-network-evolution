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

            var population = CreateInitialPopulation(rnd, seedChromosome, 20).ToImmutableHashSet();
            
            for(int i = 0; i < 10; i++) {
                population = NextPopulation(rnd, population);
            }

            Console.WriteLine(EvaluateChromosome(population.First()));
            Console.WriteLine(EvaluatePhenotype(population.First(), 2, 6));

            double EvaluateChromosome(Chromosome chromosome)
            {
                try
                {
                    var cppn = new CPPN(chromosome, inputGenes, outputGenes);

                    double fitness = 0;
                    for (int i = 0; i < 10; i++)
                        for (int j = 0; j < 5; j++)
                        {
                            var result = cppn.Calculate(ImmutableArray.Create((double)i, (double)j))[0];
                            fitness += Math.Pow(result - Math.Pow(i, j), 2);
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

            ImmutableHashSet<Chromosome> NextPopulation(Random rnd, ImmutableHashSet<Chromosome> population)
            {
                var evaluatedPopulation = population
                    .Select(chromosome => new { chromosome, fitness = EvaluateChromosome(chromosome) })
                    .OrderBy(p => p.fitness)
                    .Select(p => p.chromosome)
                    .ToImmutableList();

                var nextPopulation = evaluatedPopulation.Take(10)
                    .Concat(GetRandomOrderPopulation(rnd, population).Take(5).Select(chromosome => Mutate(chromosome, rnd)))
                    .Concat(GetRandomOrderPopulation(rnd, population).Take(5).Zip(GetRandomOrderPopulation(rnd, population).Take(5)).Select(x => Chromosome.Crossover(x.First, x.Second,
                        (ea, eb) => new EdgeGene((ea.Weight + eb.Weight) / 2, ea.IsEnabled ^ eb.IsEnabled ? rnd.NextDouble() < 0.5 : ea.IsEnabled && ea.IsEnabled)
                    )))
                    .ToImmutableHashSet();
                return nextPopulation;
            }
        }

        private static IEnumerable<Chromosome> GetRandomOrderPopulation(Random rnd, IEnumerable<Chromosome> population)
        {
            return population
                .Select(chromosome => new { chromosome, order = rnd.Next() })
                .OrderBy(p => p.order)
                .Select(p => p.chromosome);
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
                .Select(i => new NodeGene(FunctionType.Identity))
                .ToImmutableArray();
        }

        private IEnumerable<Chromosome> CreateInitialPopulation(Random rnd, Chromosome seedChromosome, int populationSize)
        {
            yield return seedChromosome;

            foreach (var i in Enumerable.Range(0, populationSize - 1))
            {
                yield return Mutate(seedChromosome, rnd);
            }
        }

        private Chromosome Mutate(Chromosome chromosome, Random rnd)
        {
            var choice = rnd.NextDouble();

            if (choice < 0.1)
            {
                return chromosome.MutateAddEdge(rnd.Next, rnd.NextDouble() * 2 - 1);
            }

            if (choice < 0.3)
            {
                return chromosome.MutateAddNode(rnd.Next, ChooseNodeType(rnd), rnd.NextDouble() * 2 - 1, rnd.NextDouble() * 2 - 1);
            }

            if (choice < 0.7)
            {
                return chromosome.MutateChangeWeight(rnd.Next, rnd.NextDouble());
            }

            return chromosome.MutateChangeEnabled(rnd.Next);
        }

        private static FunctionType ChooseNodeType(Random rnd)
        {
            var choice = rnd.NextDouble();

            if (choice < 0.1)
            {
                return FunctionType.Sin;
            }

            if (choice < 0.2)
            {
                return FunctionType.Log;
            }

            if (choice < 0.3)
            {
                return FunctionType.Exponent;
            }

            if (choice < 0.4)
            {
                return FunctionType.Heaviside;
            }

            return FunctionType.Identity;
        }
    }
}
