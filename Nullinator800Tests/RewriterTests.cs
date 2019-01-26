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

        [TestCase(@"
class C {
    /// Comment
    [CanBeNull]
    string Foo() => ""hello"";
}",
            @"
class C {
    /// Comment
    string? Foo() => ""hello"";
}",
            TestName = "PreserveComment")]

        [TestCase(@"
class C {
    public Foo([CanBeNull] string s) {}
}",
            @"
class C {
    public Foo([CanBeNull] string? s) {}
}",
            TestName = "Constructor")]

        [TestCase(@"
class C {
    public int this[[CanBeNull] string s] => 3
}",
            @"
class C {
    public int this[[CanBeNull] string? s] => 3
}",
            TestName = "Indexer")]

        public void Rewrite(string before, string expectedAfter)
        {
            var actualAfter = new NullabilityRewriter().Visit(SyntaxFactory.ParseSyntaxTree(before).GetRoot()).ToFullString();
            Assert.That(actualAfter, Is.EqualTo(expectedAfter));
        }

        [Test]
        public void Crap()
        {
            /* hello */ /* what */ Console.WriteLine("asdf");
        }
    }
}