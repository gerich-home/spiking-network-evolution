namespace SpikingNeuroEvolution
{
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
}
