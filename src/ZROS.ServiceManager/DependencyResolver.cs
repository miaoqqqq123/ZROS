using System;
using System.Collections.Generic;
using System.Linq;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager
{
    /// <summary>Represents a directed dependency graph between services.</summary>
    public class DependencyGraph
    {
        private readonly Dictionary<string, HashSet<string>> _edges = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        internal DependencyGraph() { }

        internal void AddNode(string name)
        {
            if (!_edges.ContainsKey(name))
                _edges[name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        internal void AddEdge(string from, string to)
        {
            AddNode(from);
            AddNode(to);
            _edges[from].Add(to);
        }

        /// <summary>Returns the direct dependencies of a service.</summary>
        public IEnumerable<string> GetDependencies(string name) =>
            _edges.TryGetValue(name, out var deps) ? deps : Enumerable.Empty<string>();

        /// <summary>Returns all node names in the graph.</summary>
        public IEnumerable<string> Nodes => _edges.Keys;
    }

    /// <summary>
    /// Resolves service start/stop ordering based on declared dependency relationships.
    /// </summary>
    public class DependencyResolver
    {
        /// <summary>
        /// Returns all transitive dependencies of the given service in the order they need to start.
        /// </summary>
        public List<string> ResolveDependencies(string serviceName, IEnumerable<ServiceDefinition> definitions)
        {
            if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("Service name cannot be empty.", nameof(serviceName));
            var graph = BuildDependencyGraph(definitions);
            var result = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DepthFirstVisit(serviceName, graph, visited, result, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            result.Remove(serviceName);
            return result;
        }

        /// <summary>
        /// Returns the given service names sorted in topological (startup) order.
        /// Services that other services depend on appear earlier in the list.
        /// </summary>
        public List<string> GetTopologicalOrder(IEnumerable<string> serviceNames, IEnumerable<ServiceDefinition> definitions)
        {
            if (serviceNames == null) throw new ArgumentNullException(nameof(serviceNames));
            var graph = BuildDependencyGraph(definitions);
            var nameSet = new HashSet<string>(serviceNames, StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in nameSet)
            {
                if (!visited.Contains(name))
                    DepthFirstVisit(name, graph, visited, result, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            return result.Where(n => nameSet.Contains(n)).ToList();
        }

        /// <summary>Builds a dependency graph from a collection of service definitions.</summary>
        public DependencyGraph BuildDependencyGraph(IEnumerable<ServiceDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var graph = new DependencyGraph();
            foreach (var def in definitions)
            {
                graph.AddNode(def.Name);
                foreach (var dep in def.DependsOn ?? System.Array.Empty<string>())
                    graph.AddEdge(def.Name, dep);
            }
            return graph;
        }

        /// <summary>
        /// Detects whether there is a cyclic dependency.
        /// Returns true and sets <paramref name="cycle"/> to a description of the cycle.
        /// </summary>
        public bool HasCyclicDependency(IEnumerable<ServiceDefinition> definitions, out string cycle)
        {
            var graph = BuildDependencyGraph(definitions);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in graph.Nodes)
            {
                if (DetectCycle(node, graph, visited, stack, out cycle))
                    return true;
            }

            cycle = string.Empty;
            return false;
        }

        private void DepthFirstVisit(string node, DependencyGraph graph, HashSet<string> visited, List<string> result, HashSet<string> inStack)
        {
            if (visited.Contains(node)) return;
            if (inStack.Contains(node)) return; // cycle guard

            inStack.Add(node);
            foreach (var dep in graph.GetDependencies(node))
                DepthFirstVisit(dep, graph, visited, result, inStack);

            inStack.Remove(node);
            visited.Add(node);
            result.Add(node);
        }

        private bool DetectCycle(string node, DependencyGraph graph, HashSet<string> visited, HashSet<string> stack, out string cycle)
        {
            if (stack.Contains(node))
            {
                cycle = node;
                return true;
            }
            if (visited.Contains(node))
            {
                cycle = string.Empty;
                return false;
            }

            visited.Add(node);
            stack.Add(node);

            foreach (var dep in graph.GetDependencies(node))
            {
                if (DetectCycle(dep, graph, visited, stack, out cycle))
                {
                    cycle = $"{node} -> {cycle}";
                    return true;
                }
            }

            stack.Remove(node);
            cycle = string.Empty;
            return false;
        }
    }
}
