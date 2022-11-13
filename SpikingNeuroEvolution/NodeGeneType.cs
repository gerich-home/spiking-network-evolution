using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace SpikingNeuroEvolution
{
    readonly record struct NodeGeneType
    {
        public string InnovationId {get;}

        public NodeGeneType(string innovationId)
        {
            InnovationIds.TryAdd(innovationId, 1);
            InnovationId = innovationId;
        }

        public static ConcurrentDictionary<string, byte> InnovationIds = new ConcurrentDictionary<string, byte>();

        public override string ToString() => $"Node[{ShortId}]";
        public string ShortId => ShortenId(InnovationId);

        public static string ShortenId(string id)
        {
            int maxPrefix = 0;

            foreach(var kv in InnovationIds.ToArray())
            {
                var otherId = kv.Key;
                
                if (otherId == id) {
                    continue;
                }

                var commonSize = id.TakeWhile((c, i) => c == otherId[i]).Count();
                maxPrefix = Math.Max(maxPrefix, commonSize);
            }

            return id.Substring(0, Math.Min(maxPrefix + 1, id.Length));
        }

        public static string RandomInnovationId() =>
            Hash(Guid.NewGuid().ToByteArray());

        public static string InnovationIdByParents(NodeGeneType a, NodeGeneType b, NodeGene nodeGene) =>
            Hash(a.InnovationId + b.InnovationId + nodeGene.GetHashCode());

        public static string Hash(string value) =>
            Hash(Encoding.UTF8.GetBytes(value));

        private static string Hash(byte[] buffer)
        {
            var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(buffer);

            return Convert.ToBase64String(hash);
        }
    }
}
