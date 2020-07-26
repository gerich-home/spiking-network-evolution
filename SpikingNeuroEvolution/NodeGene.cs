namespace SpikingNeuroEvolution
{
    class NodeGene
    {
        private static int GlobalInnovationNumber = 0;
        public readonly int InnovationNumber = ++GlobalInnovationNumber;
        public readonly FunctionType FunctionType;
        public readonly NodeType NodeType;
        public readonly AggregationType AggregationType;

        public override string ToString() => $"Node[{InnovationNumber}, {FunctionType}, {AggregationType}]";

        public NodeGene(FunctionType functionType, AggregationType aggregationType, NodeType nodePurpose)
        {
            FunctionType = functionType;
            AggregationType = aggregationType;
            NodeType = nodePurpose;
        }
    }
}
