namespace SpikingNeuroEvolution
{
    class EdgeGene
    {
        public readonly double Weight;
        public readonly bool IsEnabled;

        public override string ToString() => $"{Weight}{(IsEnabled ? "" : "(off)")}";

        public EdgeGene(double weight, bool isEnabled)
        {
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

        public EdgeGene ToggleEnabled()
        {
            return ChangeEnabled(!IsEnabled);
        }

        public EdgeGene ChangeEnabled(bool newIsEnabled)
        {
            return IsEnabled == newIsEnabled
                ? this
                : new EdgeGene(Weight, newIsEnabled);
        }

        public EdgeGene ChangeWeight(double newWeight)
        {
            return Weight != newWeight
                ? new EdgeGene(newWeight, IsEnabled)
                : this;
        }
    }
}
