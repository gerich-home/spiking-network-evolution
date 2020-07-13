namespace SpikingNeuroEvolution
{
    class EvaluatedChromosome
    {
        public Chromosome Chromosome { get; }
        public double Fitness { get; }

        public EvaluatedChromosome(Chromosome chromosome, double fitness)
        {
            Chromosome = chromosome;
            Fitness = fitness;
        }
    }
}
