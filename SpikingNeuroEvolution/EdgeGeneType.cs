namespace SpikingNeuroEvolution
{
    readonly record struct EdgeGeneType(NodeGeneType From, NodeGeneType To)
    {
        public override string ToString() => $"Edge[{From.ShortId}->{To.ShortId}]";
    }
}
