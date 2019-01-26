using System;
using System.Linq;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nullinator800
{
    public class NullabilityRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitParameter(ParameterSyntax p)
        {
            var isPublic =
                p.Parent.Parent is MethodDeclarationSyntax md &&
                md.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)) ||
                p.Parent.Parent is ConstructorDeclarationSyntax cd &&
                cd.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)) ||
                p.Parent.Parent is IndexerDeclarationSyntax id &&
                id.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword));

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

        static T RemoveAttribute<T>(T parent, string name) where T : SyntaxNode
        {
            var attr = FindAttribute(parent, name);
            if (attr == null)
                return parent;

            var attrList = (AttributeListSyntax)attr.Parent;

            // The attribute is part of a group which has other attributes. Just remove it.
            if (attrList.Attributes.Count > 1)
                return parent.ReplaceNode(attrList, attrList.WithAttributes(attrList.Attributes.Remove(attr)));

            // No other attributes, we need to remove the entire group.

            // Check if we're alone on the line
            /*
            var lastOnLine = 
                attrList.HasTrailingTrivia &&
                attrList.GetTrailingTrivia()
                    .SkipWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                    .FirstOrDefault()
                    .IsKind(SyntaxKind.EndOfLineTrivia);
                    */
            // Strip all whitespace trivia immediately after the attribute list.

            var newAttrList = attrList.WithTrailingTrivia(
                attrList.GetTrailingTrivia()
                    .SkipWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)));

            // TODO: This should be looking at the previous token's trailing trivia
            // to see if it ends with a newline...
            var leadingTriviaAfterWhitespace = newAttrList.GetLeadingTrivia()
                .Reverse()
                .SkipWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                .FirstOrDefault();

            var firstOnLine = leadingTriviaAfterWhitespace == default ||
                              leadingTriviaAfterWhitespace.IsKind(SyntaxKind.EndOfLineTrivia);

            var lastOnLine = newAttrList.GetTrailingTrivia().FirstOrDefault().IsKind(SyntaxKind.EndOfLineTrivia);

            // If we're alone on the line, strip out the leading whitespace and newline too
            if (firstOnLine && lastOnLine)
            {
                newAttrList = newAttrList
                    .WithTrailingTrivia(newAttrList.GetTrailingTrivia().Skip(1))
                    .WithLeadingTrivia(
                        newAttrList.GetLeadingTrivia()
                            .Reverse()
                            .SkipWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                            .Reverse());
            }

            parent = parent.ReplaceNode(attrList, newAttrList);
            parent = parent.RemoveNode(newAttrList, SyntaxRemoveOptions.KeepExteriorTrivia);
            return parent;

            // Copy the (remaining) leading and trailing trivia to the next node
            var nextNodeOrToken = parent.ChildNodesAndTokens()
                .SkipWhile(n => n != newAttrList)
                .Skip(1)
                .First();

            /*
            var nextToken = nextNodeOrToken.IsToken
                ? nextNodeOrToken.AsToken()
                : nextNodeOrToken.IsNode
                    ? nextNodeOrToken.AsNode().GetFirstToken()
                    : default;

            if (nextToken == default)
                return default;

            parent = parent.ReplaceNode(
                nextNodeOrToken,
                nextNodeOrToken.WithLeadingTrivia(

                    )
                );

            */
            throw new NotImplementedException();

            /*
var firstOnLine =
    attrList.HasLeadingTrivia &&
    attrList.GetLeadingTrivia().Last().IsKind(SyntaxKind.EndOfLineTrivia) ||
    !attrList.GetLeadingTrivia().Any() &&
    GetPreviousToken(attrList) is SyntaxToken prevToken &&
    prevToken.HasTrailingTrivia &&
    prevToken.TrailingTrivia.Last().IsKind(SyntaxKind.EndOfLineTrivia);
*/
        }

        static AttributeSyntax FindAttribute(SyntaxNode node, string name)
            => node.ChildNodes()
                .OfType<AttributeListSyntax>()
                .SelectMany(al => al.ChildNodes())
                .OfType<AttributeSyntax>()
                .FirstOrDefault(a => a.Name is SimpleNameSyntax sn && sn.Identifier.ValueText == name);

        static SyntaxToken GetPreviousToken(SyntaxNode node)
        {
            var n = node;
            while (true)
            {
                if (n.Parent == null)
                    return default;

                var children = n.Parent.ChildNodesAndTokens();
                if (node == children[0])
                {
                    n = n.Parent;
                    continue;
                }

                for (var i = 1; i < children.Count; i++)
                {
                    if (children[i] == node)
                    {
                        var not = children[i - 1];
                        return not.IsToken
                            ? not.AsToken()
                            : not.IsNode
                                ? not.AsNode().GetLastToken()
                                : default;
                    }
                }

                return default;
            }
        }
    }
}
