using System;

namespace SpikingNeuroEvolution
{

    record class NodeGene(FunctionType FunctionType, AggregationType AggregationType, NodeType NodeType)
    {

        public override string ToString() => $"{ShortNodeType}[{AggregationType}, {FunctionType}]";

        private string ShortNodeType => NodeType switch
        {
            NodeType.Inner => "",
            NodeType.Output => "OUT ",
            NodeType.Input => "IN  ",
            _ => throw new ArgumentException()
        };
    }
}
