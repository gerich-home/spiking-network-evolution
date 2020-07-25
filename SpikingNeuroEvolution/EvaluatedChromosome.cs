namespace SpikingNeuroEvolution
{
    struct EvaluatedChromosome
    {
        public readonly Chromosome Chromosome;
        public readonly double Fitness;

        public EvaluatedChromosome(Chromosome chromosome, double fitness)
        {
            Chromosome = chromosome;
            Fitness = fitness;
        }
    }
}
