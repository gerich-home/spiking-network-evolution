namespace SpikingNeuroEvolution
{
    class EdgeGene
    {
        public readonly double Weight;
        public readonly bool IsEnabled;
        public readonly EdgeGeneType GeneType;

        public EdgeGene(EdgeGeneType geneType, double weight, bool isEnabled)
        {
            GeneType = geneType;
            Weight = weight;
            IsEnabled = isEnabled;
        }

        public EdgeGene Disable()
        {
            return ChangeEnabled(false);
        }

        public EdgeGene Enable()
        {
            return ChangeEnabled(true);
        }

        public EdgeGene ChangeEnabled(bool newIsEnabled)
        {
            return IsEnabled == newIsEnabled
                ? this
                : new EdgeGene(GeneType, Weight, newIsEnabled);
        }

        public EdgeGene ChangeWeight(double newWeight)
        {
            return Weight != newWeight
                ? new EdgeGene(GeneType, newWeight, IsEnabled)
                : this;
        }
    }
}
