using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Offline syntax audit. Does not load Unity or invoke project code through reflection.
internal static class MethodAudit
{
    private static string Tokens(SyntaxNode node) => node == null ? "" : string.Join(" ", node.DescendantTokens().Select(t => t.Text));
    private static Dictionary<string, BaseMethodDeclarationSyntax> Methods(string source, string[] symbols)
    {
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(preprocessorSymbols: symbols)).GetRoot();
        var result = new Dictionary<string, BaseMethodDeclarationSyntax>();
        foreach (var method in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
        {
            var prefix = string.Join(".", method.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.Text));
            var signature = Tokens(method.WithBody(null).WithExpressionBody(null).WithSemicolonToken(default));
            result.Add(prefix + ": " + signature, method);
        }
        return result;
    }
    public static int Main(string[] args)
    {
        string before = File.ReadAllText(args[0]), after = File.ReadAllText(args[1]);
        var variants = new[] {
            new[] { "BIZZA_REAL_WITHDRAW", "BIZZA_ENABLE_ADJUST", "BIZZA_ENABLE_MAX", "DEBUG_MODE", "UNITY_EDITOR", "WKY_SDK" },
            new[] { "BIZZA_REAL_WITHDRAW", "BIZZA_ENABLE_ADJUST", "BIZZA_ENABLE_MAX", "UNITY_ANDROID", "WKY_SDK" },
            new[] { "BIZZA_REAL_WITHDRAW", "BIZZA_ENABLE_MAX", "WKY_SDK" }
        };
        var results = new List<object>();
        bool signatureOk = true;
        foreach (var symbols in variants)
        {
            var oldMethods = Methods(before, symbols);
            var newMethods = Methods(after, symbols);
            var removed = oldMethods.Keys.Except(newMethods.Keys).ToArray();
            var added = newMethods.Keys.Except(oldMethods.Keys).ToArray();
            signatureOk &= removed.Length == 0 && added.Length == 0;
            var changed = new List<object>();
            int unchanged = 0;
            foreach (var key in oldMethods.Keys.Intersect(newMethods.Keys))
            {
                var oldMethod = oldMethods[key];
                var newMethod = newMethods[key];
                if (Tokens(oldMethod.Body ?? (SyntaxNode)oldMethod.ExpressionBody) == Tokens(newMethod.Body ?? (SyntaxNode)newMethod.ExpressionBody)) { unchanged++; continue; }
                var oldCalls = oldMethod.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(Tokens).ToArray();
                var newCalls = newMethod.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(Tokens).ToArray();
                changed.Add(new {
                    signature = key,
                    beforeLine = oldMethod.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    afterLine = newMethod.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    invocationSequenceUnchanged = oldCalls.SequenceEqual(newCalls),
                    beforeBody = (oldMethod.Body ?? (SyntaxNode)oldMethod.ExpressionBody)?.ToFullString(),
                    afterBody = (newMethod.Body ?? (SyntaxNode)newMethod.ExpressionBody)?.ToFullString()
                });
            }
            results.Add(new { symbols, methods = oldMethods.Count, unchanged, removed, added, changed });
        }
        File.WriteAllText(args[2], JsonSerializer.Serialize(new { signatureOk, variants = results }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Method declaration check: " + (signatureOk ? "PASS" : "FAIL"));
        return signatureOk ? 0 : 1;
    }
}
