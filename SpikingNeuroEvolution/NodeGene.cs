namespace SpikingNeuroEvolution
{

    struct NodeGene
    {
        public readonly FunctionType FunctionType;
        public readonly NodeType NodeType;
        public readonly AggregationType AggregationType;

        public override string ToString() => $"{ShortNodeType}[{AggregationType}, {FunctionType}]";

        private string ShortNodeType => NodeType == NodeType.Inner ? "" : (NodeType == NodeType.Output ? "OUT ": "IN  ");
        public NodeGene(FunctionType functionType, AggregationType aggregationType, NodeType nodeType)
        {
            FunctionType = functionType;
            AggregationType = aggregationType;
            NodeType = nodeType;
        }
    }
}
