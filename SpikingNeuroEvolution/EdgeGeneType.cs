namespace SpikingNeuroEvolution
{
    struct EdgeGeneType
    {
        public override string ToString() => $"Edge[{From.InnovationNumber}->{To.InnovationNumber}]";
        public EdgeGeneType(NodeGene from, NodeGene to)
        {
            From = from;
            To = to;
        }

        public NodeGene From { get; }
        public NodeGene To { get; }
    }
}
