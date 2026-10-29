using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;

// Syntax-based inventory: includes conditional and historical reads, excludes comments.
// Output is used to review each study's publication contract.
var result = new List<object>();
bool verify = args.Contains("--verify");
foreach (var path in args.Where(a => a != "--verify"))
{
    var root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
    foreach (var type in root.DescendantNodes().OfType<ClassDeclarationSyntax>()
        .Where(c => c.BaseList?.Types.Any(t => t.Type.ToString() == "BaseStudy") == true))
    {
        if (verify)
        {
            var contractPath = Path.Combine(Path.GetDirectoryName(path)!, type.Identifier.ValueText + ".Publication.cs");
            var contract = CSharpSyntaxTree.ParseText(File.ReadAllText(contractPath)).GetRoot();
            var properties = contract.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                .ToDictionary(p => p.Identifier.ValueText);
            string Canonical(ExpressionSyntax e) => e.NormalizeWhitespace().ToFullString();
            var declaredReads = properties["ReadStudyKeys"].DescendantNodes().OfType<ExpressionSyntax>()
                .Select(Canonical).ToHashSet();
            var declaredWrites = properties["WrittenStudyKeys"].DescendantNodes().OfType<ExpressionSyntax>()
                .Select(Canonical).ToHashSet();
            foreach (var access in type.DescendantNodes().OfType<ElementAccessExpressionSyntax>()
                .Where(e => e.Expression is IdentifierNameSyntax id && id.Identifier.ValueText.EndsWith("Study")))
            {
                bool write = access.Parent is AssignmentExpressionSyntax a && a.Left == access;
                ExpressionSyntax key = access.ArgumentList.Arguments.Single().Expression;
                if (write && key is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.CoalesceExpression))
                {
                    key = binary.Right is ParenthesizedExpressionSyntax parens ? parens.Expression : binary.Right;
                    if (type.Identifier.ValueText == "StudyExtended" && key is BinaryExpressionSyntax extended)
                        key = extended.Right;
                }
                // LowerUpper enumerates this dependency over its configured bounds.
                if (!write && type.Identifier.ValueText == "StudyLowerUpper" && key.ToString() == "StudyMA.Label(lbl)")
                {
                    if (!properties["ReadStudyKeys"].ToString().Contains("upperCycleNdx - lowerCycleNdx + 1"))
                        throw new Exception("LowerUpper must declare its whole configured range");
                    continue;
                }
                if (!(write ? declaredWrites : declaredReads).Contains(Canonical(key)))
                    throw new Exception($"{type.Identifier}: missing {(write ? "output" : "input")} contract for {key}");
            }
            Console.WriteLine($"PASS {type.Identifier}: current/historical input and output contracts match source.");
            continue;
        }
        var accesses = type.DescendantNodes().OfType<ElementAccessExpressionSyntax>()
            .Where(e => e.Expression is IdentifierNameSyntax id && id.Identifier.ValueText.EndsWith("Study"))
            .Select(e => new {
                Key = e.ArgumentList.Arguments.Single().Expression.ToString(),
                Write = e.Parent is AssignmentExpressionSyntax a && a.Left == e,
                Start = e.SpanStart, Length = e.Span.Length,
                StatementStart = e.Ancestors().OfType<ExpressionStatementSyntax>().FirstOrDefault()?.SpanStart,
                StatementLength = e.Ancestors().OfType<ExpressionStatementSyntax>().FirstOrDefault()?.Span.Length
            }).ToArray();
        result.Add(new { Path = path, Type = type.Identifier.ValueText, Accesses = accesses });
    }
}
if (!verify) Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
