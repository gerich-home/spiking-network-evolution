namespace SpikingNeuroEvolution
{
    class EdgeGeneType
    {
        public EdgeGeneType(NodeGeneType from, NodeGeneType to)
        {
            From = from;
            To = to;
        }

        public NodeGeneType From { get; }
        public NodeGeneType To { get; }
    }
}
