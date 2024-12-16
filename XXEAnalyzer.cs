using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;

public class XXEAnalyzer : ICodeAnalyzer
{
    public void Analyze(SyntaxNode node, List<AnalysisResult> results)
    {
        if (node is ObjectCreationExpressionSyntax objectCreation)
        {
            var type = objectCreation.Type.ToString();

            if (type.Contains("XmlReader"))
                CheckXmlReaderSettings(node, results);
           
            if (type.Contains("XmlUrlResolver"))
                results.Add(CreateResult(node, "Использование XmlUrlResolver может привести к атаке XXE."));
            CheckForInputValidation(node, results);
        }
    }

    private void CheckXmlReaderSettings(SyntaxNode node, List<AnalysisResult> results)
    {
        var settings = FindXmlReaderSettings(node);
        if (settings?.Contains("DtdProcessing.Parse") == true)
            results.Add(CreateResult(node, "XML Reader используется с небезопасной настройкой DtdProcessing.Parse, что позволяет XXE-атаки."));

        if (!settings.Contains("DtdProcessing.Prohibit") && !settings.Contains("DtdProcessing.Ignore"))
            results.Add(CreateResult(node, "XML Reader не отключает обработку DTD, что может привести к XXE-атакам."));
    }

    private string FindXmlReaderSettings(SyntaxNode node)
    {
        if (node is ObjectCreationExpressionSyntax objectCreation && objectCreation.Type.ToString() == "XmlReaderSettings")
        {
            foreach (var argument in objectCreation.ArgumentList.Arguments)
            {
                if (argument.ToString().Contains("DtdProcessing"))
                    return argument.ToString();
            }
        }
        return null;
    }


    private void CheckForInputValidation(SyntaxNode node, List<AnalysisResult> results)
    {
        var validationMethods = node.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(call => call.ToString().Contains("ValidateXml") || call.ToString().Contains("Validate")).ToList();

        if (!validationMethods.Any())
            results.Add(CreateResult(node, "Отсутствует валидация входных данных XML. Примените проверку с использованием XSD-схемы."));
    }

    private AnalysisResult CreateResult(SyntaxNode node, string message) =>
        new AnalysisResult { Message = message, Location = node.GetLocation() };
}
