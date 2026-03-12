// See https://aka.ms/new-console-template for more information
using ZROS.Core;
using System;

Console.WriteLine("Hello, World!");

using var context = RosContext.Create();
Console.WriteLine($"IsOpen={context.IsOpen}, IsSimulated={context.IsSimulated}");

using var node = context.CreateNode("smoke_node");
Console.WriteLine($"Node: {node.Name} ns={node.Namespace}");
