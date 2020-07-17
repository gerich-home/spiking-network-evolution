namespace SpikingNeuroEvolution
{
    class NodeGene
    {
        private static int GlobalInnovationNumber = 0;
        public readonly int InnovationNumber = ++GlobalInnovationNumber;
        public readonly FunctionType FunctionType;

        public override string ToString() => $"Node[{InnovationNumber}, {FunctionType}]";

        public NodeGene(FunctionType functionType)
        {
            FunctionType = functionType;
        }
    }
}
