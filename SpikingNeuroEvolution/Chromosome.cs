using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class Chromosome
    {
        public readonly ImmutableDictionary<NodeGeneType, NodeGene> NodeGenes;
        public readonly ImmutableDictionary<EdgeGeneType, EdgeGene> EdgeGenes;

        public override string ToString() => $"Chromosome: {{Nodes: [{string.Join(", ", NodeGenes.Keys.Select(n => n.ShortId).OrderBy(x => x))}], Edges: [TODO]}}";
        
        public Chromosome(ImmutableDictionary<NodeGeneType, NodeGene> nodeGenes, ImmutableDictionary<EdgeGeneType, EdgeGene> edgeGenes)
        {
            CheckValid(nodeGenes, edgeGenes);
            NodeGenes = nodeGenes;
            EdgeGenes = edgeGenes;
        }

        public static Chromosome Build(
            Action<ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder, ImmutableDictionary<NodeGeneType, NodeGene>.Builder> build
        ) =>
            BuildInner(
                ImmutableDictionary.CreateBuilder<NodeGeneType, NodeGene>(),
                ImmutableDictionary.CreateBuilder<EdgeGeneType, EdgeGene>(),
                build
            );

        public Chromosome Change(
            Action<ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder, ImmutableDictionary<NodeGeneType, NodeGene>.Builder> build
        ) =>
            BuildInner(
                NodeGenes.ToBuilder(),
                EdgeGenes.ToBuilder(),
                build
            );

        public Chromosome Change(
            Action<ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder> build
        ) =>
            BuildInner(
                NodeGenes.ToBuilder(),
                EdgeGenes.ToBuilder(),
                (e, n) => build(e)
            );

        private static Chromosome BuildInner(ImmutableDictionary<NodeGeneType, NodeGene>.Builder nodeGenesBuilder, ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder edgeGenesBuilder, Action<ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder, ImmutableDictionary<NodeGeneType, NodeGene>.Builder> build)
        {
            build(edgeGenesBuilder, nodeGenesBuilder);

            return new Chromosome(
                nodeGenesBuilder.ToImmutable(),
                edgeGenesBuilder.ToImmutable()
            );
        }

        [Conditional("DEBUG")]
        private void CheckValid(
            ImmutableDictionary<NodeGeneType, NodeGene> nodeGenes,
            ImmutableDictionary<EdgeGeneType, EdgeGene> edgeGenes
        )
        {
            Debug.Assert(edgeGenes.Keys.All(edgeGeneType => nodeGenes.ContainsKey(edgeGeneType.From)));
            Debug.Assert(edgeGenes.Keys.All(edgeGeneType => nodeGenes.ContainsKey(edgeGeneType.To)));
        }

        public Chromosome MutateAddNode(
            Func<int, int> chooseEdgeGeneTypeIndex,
            NodeGene newNodeGene
        ) =>
            Change((e, n) => {
                var enabledEdges = EdgeGenes.Where(edge => edge.Value.IsEnabled).Select(edge => edge.Key).ToImmutableList();
                if (enabledEdges.Count == 0) {
                    return;
                }

                var edgeGeneType = enabledEdges.ElementAt(chooseEdgeGeneTypeIndex(enabledEdges.Count));
                var edgeGene = EdgeGenes[edgeGeneType];
                e[edgeGeneType] = edgeGene.Disable();
                var newNodeGeneType = new NodeGeneType(NodeGeneType.InnovationIdByParents(edgeGeneType.From, edgeGeneType.To, newNodeGene));
                n[newNodeGeneType] = newNodeGene;
                e[new EdgeGeneType(edgeGeneType.From, newNodeGeneType)] = new EdgeGene(1, true);
                e[new EdgeGeneType(newNodeGeneType, edgeGeneType.To)] = new EdgeGene(edgeGene.Weight, true);
            });

        public Chromosome MutateAddEdge(
            Func<int, int> chooseEdgeGeneTypeIndex,
            double weight
        ) =>
            Change((e, n) => {
                var missingEdges = NodeGenes
                    .Keys
                    .Where(fromGene => NodeGenes[fromGene].NodeType != NodeType.Output)
                    .SelectMany(fromGene => NodeGenes.Keys
                        .Where(toGene => !toGene.Equals(fromGene) && NodeGenes[toGene].NodeType != NodeType.Input)
                        .Select(toGene => new EdgeGeneType(fromGene, toGene)))
                    .ToImmutableHashSet()
                    .Except(EdgeGenes.Keys)
                    .Except(EdgeGenes.Keys.Select(key => new EdgeGeneType(key.To, key.From)));
                
                if (missingEdges.Count == 0) {
                    return;
                }

                e[missingEdges.ElementAt(chooseEdgeGeneTypeIndex(missingEdges.Count))] = new EdgeGene(weight, true);
            });

        public Chromosome MutateChangeNode(
            Func<int, int> chooseNodeGeneTypeIndex,
            Func<NodeGene, NodeGene> change
        ) =>
            Change((e, n) => {
                var innerNodes = NodeGenes
                    .Where(n => n.Value.NodeType == NodeType.Inner)
                    .ToImmutableArray();
                if (innerNodes.Length == 0) {
                    return;
                }

                var nodeGene = innerNodes.ElementAt(chooseNodeGeneTypeIndex(innerNodes.Length));
                n[nodeGene.Key] = change(nodeGene.Value);
            });

        public Chromosome MutateDeleteNode(
            Func<int, int> chooseNodeGeneTypeIndex
        ) =>
            Change((e, n) => {
                var innerNodes = NodeGenes
                    .Where(n => n.Value.NodeType == NodeType.Inner)
                    .ToImmutableArray();
                if (innerNodes.Length == 0) {
                    return;
                }

                var nodeGene = innerNodes.ElementAt(chooseNodeGeneTypeIndex(innerNodes.Length)).Key;
                n.Remove(nodeGene);

                e.RemoveRange(EdgeGenes.Keys.Where(edge => edge.From.Equals(nodeGene) || edge.To.Equals(nodeGene)));
            });

        public Chromosome MutateCollapseNode(
            Func<int, int> chooseNodeGeneTypeIndex
        ) =>
            Change((e, n) => {
                var innerNodes = NodeGenes
                    .Where(n => n.Value.NodeType == NodeType.Inner)
                    .ToImmutableArray();
                if (innerNodes.Length == 0) {
                    return;
                }

                var nodeGeneType = innerNodes.ElementAt(chooseNodeGeneTypeIndex(innerNodes.Length)).Key;
                n.Remove(nodeGeneType);

                var inputEdges = EdgeGenes.Keys.Where(edge => edge.To.Equals(nodeGeneType)).ToImmutableArray();
                var outputEdges = EdgeGenes.Keys.Where(edge => edge.From.Equals(nodeGeneType)).ToImmutableArray();
                
                var totalInput = NodeGenes[nodeGeneType].AggregationType == AggregationType.Sum ?
                    inputEdges.Select(edge => EdgeGenes[edge].ActualWeight).Sum() :
                    inputEdges.Select(edge => EdgeGenes[edge].ActualWeight).Aggregate(1.0, (x, y) => x * y);
                
                var inputNodes = inputEdges.Select(edge => edge.From).ToImmutableArray();
                var outputNodes = outputEdges.Select(edge => edge.To).ToImmutableArray();
                
                var collapsedEdges = inputNodes.SelectMany(
                    i => outputNodes.Select(
                        o => KeyValuePair.Create(
                            new EdgeGeneType(i, o),
                            new EdgeGene(totalInput * EdgeGenes[new EdgeGeneType(nodeGeneType, o)].ActualWeight, true)
                        )
                    )
                ).ToImmutableHashSet();
                e.RemoveRange(collapsedEdges.Select(edge => edge.Key));
                e.AddRange(collapsedEdges);
                
                e.RemoveRange(inputEdges);
                e.RemoveRange(outputEdges);
            });

        public Chromosome MutateChangeWeight(
            Func<int, int> chooseEdgeGeneTypeIndex,
            double weightChange
        ) =>
            Change(e => {
                if (EdgeGenes.Count == 0) {
                    return;
                }

                var edgeGeneType = EdgeGenes.Keys.ElementAt(chooseEdgeGeneTypeIndex(EdgeGenes.Count));
                var edgeGene = EdgeGenes[edgeGeneType];
                e[edgeGeneType] = edgeGene.ChangeWeight(edgeGene.Weight + weightChange);
            });

        public Chromosome MutateChangeEnabled(
            Func<int, int> chooseEdgeGeneTypeIndex
        ) =>
            Change(e => {
                if (EdgeGenes.Count == 0) {
                    return;
                }

                var edgeGeneType = EdgeGenes.Keys.ElementAt(chooseEdgeGeneTypeIndex(EdgeGenes.Count));
                var edgeGene = EdgeGenes[edgeGeneType];
                e[edgeGeneType] = edgeGene.ToggleEnabled();
            });

        public static Chromosome Crossover(
            Chromosome chromosomeA,
            Chromosome chromosomeB,
            Func<NodeGene, NodeGene, NodeGene> crossoverMatchingNodeGene,
            Func<EdgeGene, EdgeGene, EdgeGene> crossoverMatchingEdgeGene
        ) =>
            Build((e, n) => {
                var nodeGenesA = chromosomeA.NodeGenes;
                var nodeGenesB = chromosomeB.NodeGenes;

                var nodesIntersection = nodeGenesA.Keys.Intersect(nodeGenesB.Keys);

                n.AddRange(nodeGenesA.RemoveRange(nodesIntersection));
                n.AddRange(nodeGenesB.RemoveRange(nodesIntersection));

                n.AddRange(nodesIntersection
                    .Select(matchingNodeGeneType => KeyValuePair.Create(
                        matchingNodeGeneType,
                        crossoverMatchingNodeGene(nodeGenesA[matchingNodeGeneType], nodeGenesB[matchingNodeGeneType])
                    ))
                );

                var edgeGenesA = chromosomeA.EdgeGenes;
                var edgeGenesB = chromosomeB.EdgeGenes;

                var edgesIntersection = edgeGenesA.Keys.Intersect(edgeGenesB.Keys);

                e.AddRange(edgeGenesA.RemoveRange(edgesIntersection));
                e.AddRange(edgeGenesB.RemoveRange(edgesIntersection));

                e.AddRange(edgesIntersection
                    .Select(matchingEdgeGeneType => KeyValuePair.Create(
                        matchingEdgeGeneType,
                        crossoverMatchingEdgeGene(edgeGenesA[matchingEdgeGeneType], edgeGenesB[matchingEdgeGeneType])
                    ))
                );
            });

        public static double Compare(Chromosome chromosomeA, Chromosome chromosomeB, double nodeWeight, double edgeWeight)
        {
            var nodesIntersection = chromosomeA.NodeGenes.Keys.Intersect(chromosomeB.NodeGenes.Keys);
            var edgesIntersection = chromosomeA.EdgeGenes.Keys.Intersect(chromosomeB.EdgeGenes.Keys);

            var extraNodesA = chromosomeA.NodeGenes.RemoveRange(nodesIntersection);
            var extraNodesB = chromosomeB.NodeGenes.RemoveRange(nodesIntersection);

            var extraEdgesA = chromosomeA.EdgeGenes.RemoveRange(edgesIntersection);
            var extraEdgesB = chromosomeB.EdgeGenes.RemoveRange(edgesIntersection);

            // TODO
            return nodeWeight * (extraNodesA.Concat(extraNodesB).Count()) + edgeWeight * (extraEdgesA.Concat(extraEdgesB).Sum(e => Math.Abs(e.Value.ActualWeight)) + edgesIntersection.Sum(e => Math.Abs(chromosomeA.EdgeGenes[e].ActualWeight - chromosomeB.EdgeGenes[e].ActualWeight)));
        }
    }
}
