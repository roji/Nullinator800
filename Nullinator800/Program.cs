using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Buildalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Nullinator800
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                Usage();

            foreach (var arg in args)
            {
                if (arg.EndsWith(".sln"))
                {
                    var manager = new AnalyzerManager(arg);
                    foreach (var project in manager.Projects.Select(kvp => kvp.Value))
                    {
                        var results = project.Build();
                        foreach (var sourceFile in results.First().SourceFiles)
                            RewriteSourceFile(sourceFile);
                    }
                }
                else if (arg.EndsWith(".csproj"))
                {
                    var manager = new AnalyzerManager();
                    var analyzer = manager.GetProject(arg);
                    var results = analyzer.Build();
                    foreach (var sourceFile in results.First().SourceFiles)
                        RewriteSourceFile(sourceFile);
                }
                else if (arg.EndsWith(".cs"))
                    RewriteSourceFile(arg);
                else
                    Usage();
            }
        }

        static void RewriteSourceFile(string path)
        {
            SyntaxNode inTree;
            using (var f = File.OpenRead(path))
                inTree = SyntaxFactory.ParseSyntaxTree(SourceText.From(f)).GetRoot();
            var rewriter = new NullabilityRewriter();
            var outTree = rewriter.Visit(inTree);
            if (outTree != inTree)
                File.WriteAllText(path, outTree.ToFullString());
        }

        static void Usage()
        {
            var versionString = Assembly.GetEntryAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;

            Console.WriteLine($"nullinate v{versionString}");
            Console.WriteLine("-------------");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  nullinate { file.sln | file.csproj | file.cs } ...");
            Environment.Exit(1);
        }
    }
}
