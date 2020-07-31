using System;
using System.Collections.Generic;

namespace SpikingNeuroEvolution
{
    struct EdgeGeneType
    {
        public override string ToString() => $"Edge[{From.ShortId}->{To.ShortId}]";

        public override bool Equals(object obj)
        {
            return obj is EdgeGeneType type &&
                   EqualityComparer<NodeGeneType>.Default.Equals(From, type.From) &&
                   EqualityComparer<NodeGeneType>.Default.Equals(To, type.To);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(From, To);
        }

        public EdgeGeneType(NodeGeneType from, NodeGeneType to)
        {
            From = from;
            To = to;
        }

        public NodeGeneType From { get; }
        public NodeGeneType To { get; }
        
    }
}
