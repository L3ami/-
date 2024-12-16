using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


public interface ICodeAnalyzer
{
    void Analyze(SyntaxNode node, List<AnalysisResult> results);
}

public class AnalysisResult
{
    public string Message { get; set; }
    public Location Location { get; set; } 

    public override string ToString()
    {
        return $"{Message} at {Location.GetLineSpan().StartLinePosition}";
    }
}

public class CodeAnalyzer
{
    private readonly List<ICodeAnalyzer> _analyzers = new();

    public CodeAnalyzer()
    {
        
        _analyzers.Add(new SqlSecurityAnalyzer());
        _analyzers.Add(new ExceptionAnalyzer());
        _analyzers.Add(new XSSAnalyzer());
        _analyzers.Add(new XXEAnalyzer());
        _analyzers.Add(new DeserializationAnalyzer());
        _analyzers.Add(new CsrfAnalyzer());
        
    }

    public List<AnalysisResult> Analyze(SyntaxNode root)
    {
        var results = new List<AnalysisResult>();
        foreach (var analyzer in _analyzers)
        {
            analyzer.Analyze(root, results);
        }
        return results;
    }
}


class Program
{
    static void Main(string[] args)
    {

        string filePath = @"C:\code.cs";
        string code = File.ReadAllText(filePath);


        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var analyzer = new CodeAnalyzer();
        var results = analyzer.Analyze(root);


        if (results.Count == 0)
        {
            Console.WriteLine("No issues found.");
        }
        else
        {
            Console.WriteLine("Issues found:");
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }
        }
    }
}
