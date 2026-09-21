using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Offline syntax inspection only; no project reflection or runtime changes.
internal static class AuditMethods
{
    static string Tokens(SyntaxNode node) => node == null ? "" : string.Join(" ", node.DescendantTokens().Select(token => token.Text));
    static Dictionary<string, BaseMethodDeclarationSyntax> Parse(string source, string[] defines)
    {
        var root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(preprocessorSymbols: defines)).GetRoot();
        var methods = new Dictionary<string, BaseMethodDeclarationSyntax>();
        foreach (var method in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
        {
            string owner = string.Join(".", method.Ancestors().OfType<TypeDeclarationSyntax>().Reverse().Select(type => type.Identifier.Text));
            string declaration = Tokens(method.WithBody(null).WithExpressionBody(null).WithSemicolonToken(default));
            methods.Add(owner + ": " + declaration, method);
        }
        return methods;
    }
    public static int Main(string[] args)
    {
        string before = File.ReadAllText(args[0]), after = File.ReadAllText(args[1]);
        string[][] variants = {
            new[] { "BIZZA_REAL_WITHDRAW", "BIZZA_ENABLE_ADJUST", "BIZZA_ENABLE_MAX", "WKY_SDK", "UNITY_EDITOR", "DEBUG_MODE" },
            new[] { "BIZZA_REAL_WITHDRAW", "BIZZA_ENABLE_ADJUST", "BIZZA_ENABLE_MAX", "WKY_SDK", "UNITY_ANDROID" },
            new[] { "BIZZA_REAL_WITHDRAW", "BIZZA_ENABLE_MAX", "WKY_SDK" }
        };
        var reports = new List<object>();
        bool signaturesPassed = true, callsPassed = true, methodNamesPassed = true;
        foreach (var defines in variants)
        {
            var oldMethods = Parse(before, defines);
            var newMethods = Parse(after, defines);
            var removed = oldMethods.Keys.Except(newMethods.Keys).ToArray();
            var added = newMethods.Keys.Except(oldMethods.Keys).ToArray();
            signaturesPassed &= added.Length == 0 && removed.Length == 0;
            int unchanged = 0;
            var changes = new List<object>();
            foreach (string key in oldMethods.Keys.Intersect(newMethods.Keys))
            {
                var oldMethod = oldMethods[key];
                var newMethod = newMethods[key];
                var oldBody = oldMethod.Body ?? (SyntaxNode)oldMethod.ExpressionBody;
                var newBody = newMethod.Body ?? (SyntaxNode)newMethod.ExpressionBody;
                if (Tokens(oldBody) == Tokens(newBody)) { unchanged++; continue; }
                string name = (oldMethod as MethodDeclarationSyntax)?.Identifier.Text ?? "<constructor/operator>";
                bool allowed = name == "CreateFromJson" || name == "GetApplyWithdrawalRequestReal" || name == "GetApplyWithdrawalRequestFake";
                methodNamesPassed &= allowed;
                bool callsEqual = oldMethod.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(Tokens)
                    .SequenceEqual(newMethod.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(Tokens));
                callsPassed &= callsEqual;
                changes.Add(new { name, signature = key, allowedMethod = allowed, callsUnchanged = callsEqual,
                    oldBody = oldBody?.ToFullString(), newBody = newBody?.ToFullString() });
            }
            reports.Add(new { defines, methodCount = oldMethods.Count, unchanged, added, removed, changes });
        }
        bool passed = signaturesPassed && callsPassed && methodNamesPassed;
        File.WriteAllText(args[2], JsonSerializer.Serialize(new { passed, signaturesPassed, callsPassed, methodNamesPassed, variants = reports }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("Independent method audit: " + (passed ? "PASS" : "FAIL"));
        return passed ? 0 : 1;
    }
}
