using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

public class DeserializationAnalyzer : ICodeAnalyzer
{
    private readonly List<string> UnsafeDeserializationClasses = new List<string>
    {
        "BinaryFormatter",
        "SoapFormatter"
    };
    private readonly List<string> UnsafeDeserializationMethods = new List<string>
    {
        "Deserialize"
    };
    public void Analyze(SyntaxNode node, List<AnalysisResult> results)
    {
   
        var invocationExpressions = node.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocationExpressions)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null) continue;

            var methodName = memberAccess.Name.ToString();
            var objectName = memberAccess.Expression.ToString();

            
            if (UnsafeDeserializationMethods.Contains(methodName))
            {
               
                var objectCreationExpressions = node.DescendantNodes().OfType<ObjectCreationExpressionSyntax>();
                foreach (var objectCreation in objectCreationExpressions)
                {
                    var typeName = objectCreation.Type.ToString();
                    if (UnsafeDeserializationClasses.Contains(typeName) && objectName.Contains(typeName))
                    {
                        results.Add(new AnalysisResult
                        {
                            Message = $"Обнаружена потенциальная уязвимость десериализации: использование {objectName}.{methodName}(). " +
                                      "Рекомендуется заменить на более безопасные механизмы сериализации, такие как System.Text.Json.",
                            Location = memberAccess.GetLocation()
                        });
                    }
                }
            }
        }
    }
}
