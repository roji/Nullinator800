using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Nullinator800;
using NUnit.Framework;

namespace Nullinator800Tests
{
    public class RewriterTests
    {
        [Test]
        [TestCase("void Foo(string s = null);",
                  "void Foo(string? s = null);",
                  TestName = "ParamDefaultNull")]

        [TestCase("void Foo([CanBeNull] string s);",
                  "void Foo(string? s);",
                  TestName = "ParamCanBeNullPrivate")]

        [TestCase("public void Foo([CanBeNull] string s);",
                  "public void Foo([CanBeNull] string? s);",
                  TestName = "ParamCanBeNullPublic")]

        [TestCase("void Foo([NotNull] string s);",
                  "void Foo(string s);",
                  TestName = "ParamNotNullPrivate")]

        [TestCase("public void Foo([NotNull] string s);",
                  "public void Foo([NotNull] string s);",
                  TestName = "ParamNotNullPublic")]

        [TestCase("[CanBeNull] public string Foo();",
                  "[CanBeNull] public string? Foo();",
                  TestName = "ReturnValueCanBeNullPublic")]

        [TestCase("[CanBeNull] string Foo();",
                  "string? Foo();",
                  TestName = "ReturnValueCanBeNullPrivate")]

        [TestCase("    [NotNull] string Foo();",
                  "    string Foo();",
                  TestName = "ReturnValueAttrOnSameLine1")]

        [TestCase("    [NotNull] internal string Foo();",
                  "    internal string Foo();",
                  TestName = "ReturnValueAttrOnSameLine2")]

        [TestCase("    [NotNull] [SomeAttr] internal string Foo();",
                  "    [SomeAttr] internal string Foo();",
                  TestName = "ReturnValueMultipleAttributeGroups")]

        [TestCase("    [NotNull, SomeAttr] string Foo();",
                  "    [SomeAttr] string Foo();",
                  TestName = "ReturnValueMultipleAttributes")]

        [TestCase(@"
class C {
    [CanBeNull]
    string Foo();
}",
            @"
class C {
    string? Foo();
}",
            TestName = "ReturnValueAttrOnSeparateLine1")]

        public void Rewrite(string before, string expectedAfter)
        {
            var actualAfter = new NullabilityRewriter().Visit(SyntaxFactory.ParseSyntaxTree(before).GetRoot()).ToFullString();
            Assert.That(actualAfter, Is.EqualTo(expectedAfter));
        }
    }
}