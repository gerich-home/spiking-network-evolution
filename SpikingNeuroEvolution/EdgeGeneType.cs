namespace SpikingNeuroEvolution
{
    struct EdgeGeneType
    {
        public override string ToString() => $"Edge[{From.InnovationId}->{To.InnovationId}]";
        public EdgeGeneType(NodeGene from, NodeGene to)
        {
            From = from;
            To = to;
        }

        public NodeGene From { get; }
        public NodeGene To { get; }
    }
}
