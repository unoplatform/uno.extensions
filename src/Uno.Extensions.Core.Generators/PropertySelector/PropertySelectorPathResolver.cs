using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators.PropertySelector;

internal static class PropertySelectorPathResolver
{
	public static PropertySelectorPath Resolve(SimpleLambdaExpressionSyntax selector)
	{
		var paramName = selector.Parameter.Identifier.ValueText;
		var parts = new List<PropertySelectorPathPart>();

		var node = selector.ExpressionBody;
		while (node is not null)
		{
			(var part, node) = GetPart(node);
			parts.Add(part);
		}

		parts.Reverse();
		parts.RemoveAt(0); // We remove the 'e' from 'e => e.A.B.C' to keep only 'A.B.C' which is what we call the path.

		return new(string.Concat(parts.Select(part => part.Accessor)), parts);

		(PropertySelectorPathPart part, ExpressionSyntax? parent) GetPart(ExpressionSyntax node)
			=> node switch
			{
				PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppNull => Suffix(GetPart(suppNull.Operand), "!"),
				ConditionalAccessExpressionSyntax nullProp => Prefix(GetPart(nullProp.WhenNotNull), "?") with { parent = nullProp.Expression },
				MemberAccessExpressionSyntax member => Name(member, member.Name.Identifier.ValueText, member.Expression),
				MemberBindingExpressionSyntax memberBinding => Name(memberBinding, memberBinding.Name.Identifier.ValueText), // The right part the ConditionalAccessExpressionSyntax
				IdentifierNameSyntax identifier when identifier.Identifier.ValueText == paramName => default,
				IdentifierNameSyntax => throw Rules.PS0002.Fail(selector, node),
				_ => throw Rules.PS0001.Fail(selector, node),
			};

		static (PropertySelectorPathPart part, ExpressionSyntax? parent) Prefix((PropertySelectorPathPart part, ExpressionSyntax? parent) inner, string accessorPrefix)
			=> (inner.part with { Accessor = accessorPrefix + inner.part.Accessor }, inner.parent);

		static (PropertySelectorPathPart part, ExpressionSyntax? parent) Suffix((PropertySelectorPathPart part, ExpressionSyntax? parent) inner, string accessorSuffix)
			=> (inner.part with { Accessor = inner.part.Accessor + accessorSuffix }, inner.parent);

		static (PropertySelectorPathPart part, ExpressionSyntax? parent) Name(SyntaxNode node, string name, ExpressionSyntax? parent = null)
			=> (new PropertySelectorPathPart(node, name, "." + name), parent);
	}
}
