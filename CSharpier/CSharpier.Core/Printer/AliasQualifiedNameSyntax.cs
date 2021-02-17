using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpier.Core
{
    public partial class Printer
    {
        private Doc PrintAliasQualifiedNameSyntax(AliasQualifiedNameSyntax node)
        {
            return Concat(this.Print(node.Alias), this.PrintSyntaxToken(node.ColonColonToken), this.Print(node.Name));
        }
    }
}