namespace SpikingNeuroEvolution
{
    struct EdgeGeneType
    {
        public EdgeGeneType(NodeGene from, NodeGene to)
        {
            From = from;
            To = to;
        }

        public NodeGene From { get; }
        public NodeGene To { get; }
    }
}
