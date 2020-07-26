using System;
using System.Security.Cryptography;
using System.Text;

namespace SpikingNeuroEvolution
{
    struct NodeGene
    {
        public readonly FunctionType FunctionType;
        public readonly NodeType NodeType;
        public readonly AggregationType AggregationType;
        public readonly string InnovationId;

        public override string ToString() => $"Node[{InnovationId}, {FunctionType}, {AggregationType}]";

        public NodeGene(string innovationId, FunctionType functionType, AggregationType aggregationType, NodeType nodePurpose)
        {
            InnovationId = innovationId;
            FunctionType = functionType;
            AggregationType = aggregationType;
            NodeType = nodePurpose;
        }

        public static string RandomInnovationId() =>
            Hash(Guid.NewGuid().ToByteArray());

        public static string InnovationIdByParents(NodeGene a, NodeGene b) =>
            Hash(Encoding.UTF8.GetBytes(a.InnovationId + b.InnovationId));

        private static string Hash(byte[] buffer)
        {
            var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(buffer);

            return Convert.ToBase64String(hash);
        }
    }
}
