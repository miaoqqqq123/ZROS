using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZROS.ServiceManager;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.Tests
{
    public class DependencyResolverTests
    {
        private readonly DependencyResolver _resolver = new DependencyResolver();

        private static ServiceDefinition Svc(string name, params string[] deps) =>
            new ServiceDefinition { Name = name, DependsOn = deps };

        [Fact]
        public void GetTopologicalOrder_NoDeps_ReturnsSingleService()
        {
            var defs = new[] { Svc("a") };
            var order = _resolver.GetTopologicalOrder(new[] { "a" }, defs);
            Assert.Single(order);
            Assert.Equal("a", order[0]);
        }

        [Fact]
        public void GetTopologicalOrder_LinearChain_ReturnsCorrectOrder()
        {
            // a -> b -> c  (a depends on b, b depends on c)
            var defs = new[] { Svc("a", "b"), Svc("b", "c"), Svc("c") };
            var order = _resolver.GetTopologicalOrder(new[] { "a", "b", "c" }, defs);
            // c must come before b, b before a
            Assert.True(order.IndexOf("c") < order.IndexOf("b"));
            Assert.True(order.IndexOf("b") < order.IndexOf("a"));
        }

        [Fact]
        public void GetTopologicalOrder_Diamond_ReturnsCorrectOrder()
        {
            // d depends on b and c; b and c depend on a
            var defs = new[]
            {
                Svc("a"),
                Svc("b", "a"),
                Svc("c", "a"),
                Svc("d", "b", "c")
            };
            var order = _resolver.GetTopologicalOrder(new[] { "a", "b", "c", "d" }, defs);
            Assert.True(order.IndexOf("a") < order.IndexOf("b"));
            Assert.True(order.IndexOf("a") < order.IndexOf("c"));
            Assert.True(order.IndexOf("b") < order.IndexOf("d"));
            Assert.True(order.IndexOf("c") < order.IndexOf("d"));
        }

        [Fact]
        public void ResolveDependencies_ReturnsTransitiveDeps()
        {
            var defs = new[] { Svc("a", "b"), Svc("b", "c"), Svc("c") };
            var deps = _resolver.ResolveDependencies("a", defs);
            Assert.Contains("b", deps);
            Assert.Contains("c", deps);
            Assert.DoesNotContain("a", deps);
        }

        [Fact]
        public void HasCyclicDependency_NoCycle_ReturnsFalse()
        {
            var defs = new[] { Svc("a", "b"), Svc("b", "c"), Svc("c") };
            Assert.False(_resolver.HasCyclicDependency(defs, out _));
        }

        [Fact]
        public void HasCyclicDependency_DirectCycle_ReturnsTrue()
        {
            var defs = new[] { Svc("a", "b"), Svc("b", "a") };
            Assert.True(_resolver.HasCyclicDependency(defs, out var cycle));
            Assert.False(string.IsNullOrWhiteSpace(cycle));
        }

        [Fact]
        public void HasCyclicDependency_IndirectCycle_ReturnsTrue()
        {
            var defs = new[] { Svc("a", "b"), Svc("b", "c"), Svc("c", "a") };
            Assert.True(_resolver.HasCyclicDependency(defs, out var cycle));
            Assert.False(string.IsNullOrWhiteSpace(cycle));
        }

        [Fact]
        public void BuildDependencyGraph_ContainsAllNodes()
        {
            var defs = new[] { Svc("a"), Svc("b", "a"), Svc("c", "a") };
            var graph = _resolver.BuildDependencyGraph(defs);
            Assert.Contains("a", graph.Nodes);
            Assert.Contains("b", graph.Nodes);
            Assert.Contains("c", graph.Nodes);
        }

        [Fact]
        public void BuildDependencyGraph_ContainsCorrectEdges()
        {
            var defs = new[] { Svc("a"), Svc("b", "a") };
            var graph = _resolver.BuildDependencyGraph(defs);
            Assert.Contains("a", graph.GetDependencies("b"));
        }
    }
}
