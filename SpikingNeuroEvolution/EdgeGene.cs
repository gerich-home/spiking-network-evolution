namespace SpikingNeuroEvolution
{
    record class EdgeGene(double Weight, bool IsEnabled)
    {
        public override string ToString() => $"{Weight}{(IsEnabled ? "" : "(off)")}";

        public double ActualWeight => IsEnabled ? Weight : 0;

        public EdgeGene Disable() => ChangeEnabled(false);

        public EdgeGene Enable() => ChangeEnabled(true);

        public EdgeGene ToggleEnabled() => ChangeEnabled(!IsEnabled);

        public EdgeGene ChangeEnabled(bool newIsEnabled)
            => this with {IsEnabled = newIsEnabled};

        public EdgeGene ChangeWeight(double newWeight)
            => this with {Weight = newWeight};
    }
}
