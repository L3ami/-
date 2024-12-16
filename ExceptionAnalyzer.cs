using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

public class ExceptionAnalyzer : ICodeAnalyzer
{
    public void Analyze(SyntaxNode node, List<AnalysisResult> results)
    {
        var catchClauses = node.DescendantNodes().OfType<CatchClauseSyntax>();
        foreach (var catchClause in catchClauses)
        {
            if (catchClause.Declaration?.Type.ToString() == "Exception")
            {
                results.Add(new AnalysisResult
                {
                    Message = "Обнаружен общий обработчик исключений catch (Exception). Рекомендуется использовать более конкретный тип исключения.",
                    Location = catchClause.GetLocation()
                });
            }
            var statements = catchClause.Block.Statements;
            if (statements.Count == 0 || statements.All(stmt => stmt is EmptyStatementSyntax || stmt.ToString().StartsWith("//")))
            {
                results.Add(new AnalysisResult
                {
                    Message = "Обнаружен пустой или нефункциональный блок catch. Добавьте логику для обработки исключений.",
                    Location = catchClause.GetLocation()
                });
            }
            var logStatements = catchClause.Block.Statements
                .Where(stmt => stmt.ToString().Contains("ex.Message") || stmt.ToString().Contains("ex.StackTrace"));

            foreach (var log in logStatements)
            {
                results.Add(new AnalysisResult
                {
                    Message = "Обнаружено логирование исключения (ex.Message или ex.StackTrace). Это может привести к утечке конфиденциальных данных.",
                    Location = log.GetLocation()
                });
            }
        }

        var methods = node.DescendantNodes().OfType<MethodDeclarationSyntax>();
        foreach (var method in methods)
        {
            var criticalOperations = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(inv => inv.ToString().Contains("File") || inv.ToString().Contains("HttpClient") || inv.ToString().Contains("Sql"));
            if (criticalOperations.Any() && !method.DescendantNodes().OfType<TryStatementSyntax>().Any())
            {
                results.Add(new AnalysisResult
                {
                    Message = $"Метод '{method.Identifier}' содержит критические операции, но отсутствует блок try-catch. Добавьте обработку исключений.",
                    Location = method.GetLocation()
                });
            }
        }
    }
}
