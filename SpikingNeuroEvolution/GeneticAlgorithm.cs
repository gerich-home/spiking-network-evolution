using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class GeneticAlgorithm
    {
        public void Test()
        {
            var (seedChromosome, inputGenes, outputGenes) = CreateSeedChromosome(3, 1);

            var rnd = new Random();

            const int populationSize = 300;
            const double mutantsFraction = 0.3;
            const double childrenFraction = 0.06;
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
                Console.WriteLine($"nodes={best.NodeGenes.Count - (inputGenes.Length + outputGenes.Length)} edges={best.EdgeGenes.Count(e => e.Value.IsEnabled)} -> distance (inv fitness): {1000 / goldEvaluation.Fitness(best)}. avg: {goldEvaluation.AliveChromosomes.Average(x => 1000 / goldEvaluation.Fitness(x))}");

                foreach(var example in goldExamples)
                {
                    var (x, y) = example;
                    Console.WriteLine($"f({x}, {y}) -> {EvaluatePhenotype(best, example)} vs {TargetFunction(x, y)}");
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
                            return 0;
                        } else {
                            worst = Math.Max(worst, Math.Abs(result - TargetFunction(a, b)));
                        }
                    }

                    if (worst == 0) {
                        return double.PositiveInfinity;
                    }

                    return 1000 / worst;
                }
                catch(LoopInCPPNException)
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
                var comparisonParams = new ChromosomeComparisonParams(1, 2, 4);
                var speciesSet = SpeciesSet.Build(evaluatedPopulation.AliveChromosomes, comparisonParams);

                Console.WriteLine($"Species: {speciesSet.Count}");

                var sizes = speciesSet.AllSpecies.ToDictionary(species => species.Size);
                var speciesFitnesses = speciesSet.AllSpecies.ToDictionary(species => species.AverageFitness(evaluatedPopulation));
                var totalFitness = speciesFitnesses.Sum(p => p.Value);
                var fitnessCost = populationSize / totalFitness;

                var guaranteedSizes = speciesFitnesses.MapValues(fitness => (int)(fitness * fitnessCost));
                var remainingSize = populationSize - guaranteedSizes.Values.Sum();
                
                var speciesGotExtraIndividual = speciesFitnesses
                    .MapValues(fitness =>
                    {
                        var v = fitness * fitnessCost;
                        return v - (int)v;
                    })
                    .OrderByDescending(pair => pair.Value)
                    .Take(remainingSize)
                    .Select(pair => pair.Key)
                    .ToImmutableHashSet();

                var chromosomes = speciesSet.AllSpecies
                    .SelectMany(species =>
                    {
                        var allocatedSize = guaranteedSizes[species] + (speciesGotExtraIndividual.Contains(species) ? 1 : 0);
                        
                        var mutantsCount = (int)(Math.Round(allocatedSize * mutantsFraction));
                        var childrenCount = (int)(Math.Round(allocatedSize * childrenFraction));

                        var eliteCount = Math.Min(species.Chromosomes.Count, allocatedSize - (mutantsCount + childrenCount));

                        var elite = species.Chromosomes
                            .OrderByDescending(evaluatedPopulation.Fitness)
                            .Take(eliteCount);

                        var randomChromosomes = species.Chromosomes.RandomlyRepeated(rnd);
                        var mutants = randomChromosomes.Select(Mutate).Take(allocatedSize - (eliteCount + childrenCount));
                        var randomParents = randomChromosomes.Take(childrenCount);
                        var children = randomParents.Zip(randomParents).Select(Crossover);

                        var result = elite.Concat(mutants).Concat(children).ToImmutableList();
                        
                        Debug.Assert(result.Count == allocatedSize);
                        Debug.Assert(result.Count > 0);
                        return result;
                    })
                    .ToImmutableList();

                Debug.Assert(chromosomes.Count == populationSize);

                return EvaluatePopulation(chromosomes);
            }

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

                if (choice < 0.2)
                {
                    return chromosome.MutateAddNode(rnd.Next, RandomNode());
                }

                if (choice < 0)
                {
                    return chromosome.MutateChangeNode(rnd.Next, n => RandomNode());
                }

                if (choice < 0.4)
                {
                    return chromosome.MutateAddEdge(rnd.Next, rnd.NextGaussian(0, 2));
                }

                if (choice < 0)
                {
                    return chromosome.MutateCollapseNode(rnd.Next);
                }

                if (choice < 0)
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
