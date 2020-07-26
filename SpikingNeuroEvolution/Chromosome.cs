using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SpikingNeuroEvolution
{
    class Chromosome
    {
        public readonly ImmutableHashSet<NodeGene> NodeGenes;
        public readonly ImmutableDictionary<EdgeGeneType, EdgeGene> EdgeGenes;

        public override string ToString() => $"Chromosome: {{Nodes: [{string.Join(", ", NodeGenes.Select(n => n.InnovationNumber).OrderBy(x => x))}], Edges: []}}";
        
        public Chromosome(ImmutableHashSet<NodeGene> nodeGenes, ImmutableDictionary<EdgeGeneType, EdgeGene> edgeGenes)
        {
            CheckValid(nodeGenes, edgeGenes);
            NodeGenes = nodeGenes;
            EdgeGenes = edgeGenes;
        }

        public static Chromosome Build(
            Action<ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder, ImmutableHashSet<NodeGene>.Builder> build
        ) =>
            BuildInner(
                ImmutableHashSet.CreateBuilder<NodeGene>(),
                ImmutableDictionary.CreateBuilder<EdgeGeneType, EdgeGene>(),
                build
            );

        public Chromosome Change(
            Action<ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder, ImmutableHashSet<NodeGene>.Builder> build
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

        private static Chromosome BuildInner(ImmutableHashSet<NodeGene>.Builder nodeGenesBuilder, ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder edgeGenesBuilder, Action<ImmutableDictionary<EdgeGeneType, EdgeGene>.Builder, ImmutableHashSet<NodeGene>.Builder> build)
        {
            build(edgeGenesBuilder, nodeGenesBuilder);

            return new Chromosome(
                nodeGenesBuilder.ToImmutable(),
                edgeGenesBuilder.ToImmutable()
            );
        }

        [Conditional("DEBUG")]
        private void CheckValid(ImmutableHashSet<NodeGene> nodeGenes, ImmutableDictionary<EdgeGeneType, EdgeGene> edgeGenes)
        {
            Debug.Assert(edgeGenes.Keys.All(edgeGeneType => nodeGenes.Contains(edgeGeneType.From)));
            Debug.Assert(edgeGenes.Keys.All(edgeGeneType => nodeGenes.Contains(edgeGeneType.To)));
        }

        public Chromosome MutateAddNode(
            Func<int, int> chooseEdgeGeneTypeIndex,
            FunctionType functionType,
            AggregationType aggregationType,
            double fromWeight,
            double toWeight
        ) =>
            Change((e, n) => {
                var newNodeGene = new NodeGene(functionType, aggregationType, NodeType.Inner);
                n.Add(newNodeGene);
                var edgeGeneType = EdgeGenes.Keys.ElementAt(chooseEdgeGeneTypeIndex(EdgeGenes.Count));
                var edgeGene = EdgeGenes[edgeGeneType];
                e[edgeGeneType] = edgeGene.Disable();
                e[new EdgeGeneType(edgeGeneType.From, newNodeGene)] = new EdgeGene(fromWeight, true);
                e[new EdgeGeneType(newNodeGene, edgeGeneType.To)] = new EdgeGene(toWeight, true);
            });

        public Chromosome MutateAddEdge(
            Func<int, int> chooseEdgeGeneTypeIndex,
            double weight
        ) =>
            Change((e, n) => {
                var missingEdges = NodeGenes
                    .Where(fromGene => fromGene.NodeType != NodeType.Output)
                    .SelectMany(fromGene => NodeGenes
                        .Where(toGene => toGene != fromGene && toGene.NodeType != NodeType.Input)
                        .Select(toGene => new EdgeGeneType(fromGene, toGene)))
                    .ToImmutableHashSet()
                    .Except(EdgeGenes.Keys)
                    .Except(EdgeGenes.Keys.Select(key => new EdgeGeneType(key.To, key.From)));
                
                if (missingEdges.Count == 0) {
                    return;
                }

                e[missingEdges.ElementAt(chooseEdgeGeneTypeIndex(missingEdges.Count))] = new EdgeGene(weight, true);
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
            Func<EdgeGene, EdgeGene, EdgeGene> crossoverMatchingGene
        ) =>
            Build((e, n) => {
                n.UnionWith(chromosomeA.NodeGenes);
                n.UnionWith(chromosomeB.NodeGenes);

                var edgeGenesA = chromosomeA.EdgeGenes;
                var edgeGenesB = chromosomeB.EdgeGenes;

                var intersection = edgeGenesA.Keys.Intersect(edgeGenesB.Keys);

                e.AddRange(edgeGenesA.RemoveRange(intersection));
                e.AddRange(edgeGenesB.RemoveRange(intersection));

                e.AddRange(intersection
                    .Select(matchingEdgeGeneType => KeyValuePair.Create(
                        matchingEdgeGeneType,
                        crossoverMatchingGene(edgeGenesA[matchingEdgeGeneType], edgeGenesB[matchingEdgeGeneType])
                    ))
                );
            });

        public static double Compare(Chromosome chromosomeA, Chromosome chromosomeB, double nodeWeight, double edgeWeight)
        {
            var diffingNodes = chromosomeA.NodeGenes.SymmetricExcept(chromosomeB.NodeGenes);
            var intersection = chromosomeA.EdgeGenes.Keys.Intersect(chromosomeB.EdgeGenes.Keys);

            var extraEdgesA = chromosomeA.EdgeGenes.RemoveRange(intersection);
            var extraEdgesB = chromosomeB.EdgeGenes.RemoveRange(intersection);

            return nodeWeight * diffingNodes.Count + edgeWeight * (extraEdgesA.Concat(extraEdgesB).Sum(e => Math.Abs(e.Value.ActualWeight)) + intersection.Sum(e => Math.Abs(chromosomeA.EdgeGenes[e].ActualWeight - chromosomeB.EdgeGenes[e].ActualWeight)));
        }
    }
}
