using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nullinator800
{
    public class NullabilityRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitParameter(ParameterSyntax p)
        {
            var isPublic = p.Parent.Parent is MethodDeclarationSyntax md &&
                           md.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword));

            // If the parameter has default null, make it nullable
            if (p.Default?.Value is LiteralExpressionSyntax le && le.IsKind(SyntaxKind.NullLiteralExpression))
                p = p.WithType(WrapWithNullable(p.Type));

            // If [CanBeNull] exists, make the parameter nullable
            if (FindAttribute(p, "CanBeNull") != null)
                p = p.WithType(WrapWithNullable(p.Type));

            // If [NotNull] and/or [CanBeNull] exist and the method isn't public, remove them
            // If the method isn't public, remove [NotNull] and/or [CanBeNull]
            if (!isPublic)
            {
                p = RemoveAttribute(p, "CanBeNull");
                p = RemoveAttribute(p, "NotNull");
            }

            return base.VisitParameter(p);
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax m)
        {
            var isPublic = m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword));

            // If [CanBeNull] exists, make the return type nullable
            if (FindAttribute(m, "CanBeNull") != null)
                m = m.WithReturnType(WrapWithNullable(m.ReturnType));

            // If the method isn't public, remove [NotNull] and/or [CanBeNull]
            if (!isPublic)
            {
                m = RemoveAttribute(m, "NotNull");
                m = RemoveAttribute(m, "CanBeNull");
            }

            return base.VisitMethodDeclaration(m);
        }

        static TypeSyntax WrapWithNullable(TypeSyntax t)
            => t is NullableTypeSyntax ? t : SyntaxFactory.NullableType(t.WithoutTrivia()).WithTriviaFrom(t);

        static T RemoveAttribute<T>(T node, string name) where T : SyntaxNode
        {
            var attr = FindAttribute(node, name);
            if (attr == null)
                return node;

            var attrList = (AttributeListSyntax)attr.Parent;
            return attrList.Attributes.Count == 1
                ? node.RemoveNode(attrList,
                    node.HasTrailingTrivia &&
                    node.GetTrailingTrivia().Last() is SyntaxTrivia tr &&
                    tr.IsKind(SyntaxKind.EndOfLineTrivia)
                        ? SyntaxRemoveOptions.KeepNoTrivia
                        : SyntaxRemoveOptions.KeepLeadingTrivia)
                : node.ReplaceNode(attrList, attrList.WithAttributes(attrList.Attributes.Remove(attr)));
        }

        static AttributeSyntax FindAttribute(SyntaxNode node, string name)
            => node.ChildNodes()
                .OfType<AttributeListSyntax>()
                .SelectMany(al => al.ChildNodes())
                .OfType<AttributeSyntax>()
                .FirstOrDefault(a => a.Name is SimpleNameSyntax sn && sn.Identifier.ValueText == name);
    }
}
