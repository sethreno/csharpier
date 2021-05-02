using System.Linq;
using CSharpier.DocTypes;
using CSharpier.SyntaxPrinter;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpier.SyntaxPrinter.SyntaxNodePrinters
{
    public static class RecursivePattern
    {
        public static Doc Print(RecursivePatternSyntax node)
        {
            return Doc.Concat(
                node.Type != null ? Node.Print(node.Type) : Doc.Null,
                node.PositionalPatternClause != null
                    ? Doc.Concat(
                            Token.Print(
                                node.PositionalPatternClause.OpenParenToken
                            ),
                            SeparatedSyntaxList.Print(
                                node.PositionalPatternClause.Subpatterns,
                                subpatternNode =>
                                    Doc.Concat(
                                        subpatternNode.NameColon != null
                                            ? NameColon.Print(
                                                    subpatternNode.NameColon
                                                )
                                            : string.Empty,
                                        Node.Print(subpatternNode.Pattern)
                                    ),
                                " "
                            ),
                            Token.Print(
                                node.PositionalPatternClause.CloseParenToken
                            )
                        )
                    : string.Empty,
                node.PropertyPatternClause != null
                    ? Doc.Concat(
                            " ",
                            Token.PrintWithSuffix(
                                node.PropertyPatternClause.OpenBraceToken,
                                " "
                            ),
                            SeparatedSyntaxList.Print(
                                node.PropertyPatternClause.Subpatterns,
                                subpatternNode =>
                                    Doc.Concat(
                                        subpatternNode.NameColon != null
                                            ? NameColon.Print(
                                                    subpatternNode.NameColon
                                                )
                                            : Doc.Null,
                                        Node.Print(subpatternNode.Pattern)
                                    ),
                                " "
                            ),
                            " ",
                            Token.PrintWithSuffix(
                                node.PropertyPatternClause.CloseBraceToken,
                                " "
                            )
                        )
                    : string.Empty,
                node.Designation != null
                    ? Node.Print(node.Designation)
                    : string.Empty
            );
        }
    }
}