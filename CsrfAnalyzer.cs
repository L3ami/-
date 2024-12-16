using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class CsrfAnalyzer : ICodeAnalyzer
{
    public void Analyze(SyntaxNode node, List<AnalysisResult> results)
    {
        var methods = node.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            var attributes = method.AttributeLists.SelectMany(a => a.Attributes).ToList();
            var hasValidateToken = attributes.Any(a => a.Name.ToString().Contains("ValidateAntiForgeryToken"));

            if (!hasValidateToken && IsHttpPostMethod(method))
            {
                results.Add(new AnalysisResult
                {
                    Message = "Метод, обрабатывающий POST-запросы, не имеет атрибута [ValidateAntiForgeryToken]",
                    Location = method.GetLocation()
                });
            }
        }

        foreach (var method in methods)
        {
            var attributes = method.AttributeLists.SelectMany(a => a.Attributes).ToList();
            var hasHttpGet = attributes.Any(a => a.Name.ToString().Contains("HttpGet"));

            if (hasHttpGet && MethodModifiesState(method))
            {
                results.Add(new AnalysisResult
                {
                    Message = "Метод с [HttpGet] изменяет состояние приложения, используйте [HttpPost]",
                    Location = method.GetLocation()
                });
            }
        }

        var ifStatements = node.DescendantNodes().OfType<IfStatementSyntax>();
        bool hasOriginCheck = ifStatements.Any(ifStmt =>
            ifStmt.Condition.ToString().Contains("Request.Headers[\"Origin\"]") ||
            ifStmt.Condition.ToString().Contains("Request.Headers[\"Referer\"]"));

        if (!hasOriginCheck && IsHttpPostMethod(methods.FirstOrDefault()))
        {
            results.Add(new AnalysisResult
            {
                Message = "В коде отсутствует проверка заголовков Origin или Referer",
                Location = node.GetLocation()
            });
        }


        var cookieAppendCalls = node.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => inv.Expression.ToString().Contains("HttpContext.Response.Cookies.Append"));

        foreach (var call in cookieAppendCalls)
        {
            if (!call.ToString().Contains("SameSite"))
            {
                results.Add(new AnalysisResult
                {
                    Message = "У куки отсутствует атрибут SameSite",
                    Location = call.GetLocation()
                });
            }
        }

        
    }

    private bool IsHttpPostMethod(MethodDeclarationSyntax method)
    {
        return method.AttributeLists.SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString().Contains("HttpPost"));
    }

    private bool MethodModifiesState(MethodDeclarationSyntax method)
    {
        return method.Body != null && method.Body.Statements
            .Any(stmt => stmt.ToString().Contains("Change") || stmt.ToString().Contains("Update"));
    }
}