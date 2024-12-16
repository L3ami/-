using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class SqlSecurityAnalyzer : ICodeAnalyzer
{
    private readonly List<string> _unsafeMethods = new List<string> { "ExecuteReader", "ExecuteScalar", "ExecuteNonQuery" };

    private const string SqlCommandType = "SqlCommand";
    private const string SqlParameterType = "SqlParameter";
    private const string ValidateMethod = "Validate";
    private const string SqlInjectionMessage = "Возможная уязвимость SQL инъекции";

    private static readonly Regex DropTruncateRegex = new Regex(@"\b(DROP|TRUNCATE|DELETE)\b", RegexOptions.IgnoreCase);
    private static readonly Regex InjectionPatternRegex = new Regex(@"IN\s*\(.*\)", RegexOptions.IgnoreCase);

    public void Analyze(SyntaxNode node, List<AnalysisResult> results)
    {
        CheckStringConcatenation(node, results);

        CheckSqlCommands(node, results);

        CheckDangerousSqlOperators(node, results);

        CheckSqlInjectionThroughIn(node, results);
    }

    private void CheckStringConcatenation(SyntaxNode node, List<AnalysisResult> results)
    {
        var binaryExpressions = node.DescendantNodes().OfType<BinaryExpressionSyntax>();
        foreach (var expr in binaryExpressions)
        {
            if (expr.OperatorToken.Text == "+" && expr.Left.ToString().Contains(SqlCommandType))
            {
                results.Add(new AnalysisResult
                {
                    Message = "Обнаружена строковая конкатенация в SQL-запросе",
                    Location = expr.GetLocation()
                });
            }
        }
    }

    private void CheckSqlCommands(SyntaxNode node, List<AnalysisResult> results)
    {
        var methodInvocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
        foreach (var invocation in methodInvocations)
        {
            var methodName = invocation.Expression.ToString();

            
            if (methodName.Contains(SqlCommandType) && !invocation.ToString().Contains("@"))
            {
                AddAnalysisResult(results, "SQL-запрос без параметризации найден", invocation);
            }

            
            if (methodName.Contains(SqlCommandType) && !invocation.ToString().Contains(SqlParameterType))
            {
                AddAnalysisResult(results, "Возможное отсутствие экранирования данных в SQL-запросе", invocation);
            }

            
            if (methodName.Contains(SqlCommandType) && !invocation.ToString().Contains(ValidateMethod))
            {
                AddAnalysisResult(results, "Отсутствует валидация данных пользователя в SQL-запросе", invocation);
            }
        }
    }

    private void CheckDangerousSqlOperators(SyntaxNode node, List<AnalysisResult> results)
    {
        var stringLiterals = node.DescendantNodes().OfType<LiteralExpressionSyntax>();
        foreach (var literal in stringLiterals)
        {
            var literalText = literal.Token.Text;

            
            if (DropTruncateRegex.IsMatch(literalText))
            {
                results.Add(new AnalysisResult
                {
                    Message = "Обнаружены опасные SQL-операторы (DROP, TRUNCATE, DELETE)",
                    Location = literal.GetLocation()
                });
            }
        }
    }

    private void CheckSqlInjectionThroughIn(SyntaxNode node, List<AnalysisResult> results)
    {
        var stringLiterals = node.DescendantNodes().OfType<LiteralExpressionSyntax>();
        foreach (var literal in stringLiterals)
        {
            var literalText = literal.Token.Text;

           
            if (InjectionPatternRegex.IsMatch(literalText))
            {
                results.Add(new AnalysisResult
                {
                    Message = "Обнаружена возможность SQL-инъекции через конструкцию IN",
                    Location = literal.GetLocation()
                });
            }
        }
    }

    private void AddAnalysisResult(List<AnalysisResult> results, string message, SyntaxNode node)
    {
        results.Add(new AnalysisResult
        {
            Message = $"{SqlInjectionMessage}: {message}",
            Location = node.GetLocation()
        });
    }
}
