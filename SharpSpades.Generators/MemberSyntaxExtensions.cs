using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#nullable enable

namespace SharpSpades.Generators
{
    internal static class MemberSyntaxExtensions
    {
        public static bool HasAttribute(this MemberDeclarationSyntax member, string attributeName)
            => member.AttributeLists.SelectMany(list => list.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == attributeName) is not null;
    }
}