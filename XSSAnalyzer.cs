using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;


public class XSSAnalyzer : ICodeAnalyzer
{
    public void Analyze(SyntaxNode node, List<AnalysisResult> results)
    {
        var invocationExpressions = node.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocationExpressions)
        {
            var methodName = invocation.Expression.ToString();
            if (methodName.Contains("Response.Write") || methodName.Contains("innerHTML"))
            {
                var arguments = invocation.ArgumentList.Arguments;
                if (!ArgumentsContainValidation(arguments))
                {
                    results.Add(new AnalysisResult
                    {
                        Message = $"Обнаружен потенциально уязвимый вызов {methodName}. Рекомендуется экранировать входные данные.",
                        Location = invocation.GetLocation()
                    });
                }
            }
        }
    }
    private bool ArgumentsContainValidation(SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument.ToString().Contains("HtmlEncode") || argument.ToString().Contains("Sanitize"))
            {
                return true;
            }
        }
        return false;
    }
}

