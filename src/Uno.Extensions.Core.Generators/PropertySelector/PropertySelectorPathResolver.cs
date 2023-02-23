using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.Extensions.Generators.PropertySelector;

internal static class PropertySelectorPathResolver
{
	private sealed class Visitor : CSharpSyntaxVisitor
	{
		private bool _visitingWhenNotNull;

		public List<PropertySelectorPathPart> Parts { get; } = new();

		private PropertySelectorPathPart CreatePathPart(string name)
		{
			var prefix = _visitingWhenNotNull ? "?." : ".";
			_visitingWhenNotNull = false;
			return new(name, prefix + name);
		}

		public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
		{
			Visit(node.Expression);
			_visitingWhenNotNull = true;
			Visit(node.WhenNotNull);
		}

		public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
		{
			Visit(node.Expression);
			var identifier = node.Name.Identifier.ValueText;
			Parts.Add(CreatePathPart(identifier));
		}

		public override void VisitMemberBindingExpression(MemberBindingExpressionSyntax node)
		{
			var identifier = node.Name.Identifier.ValueText;
			Parts.Add(CreatePathPart(identifier));
		}

		public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
		{
			Visit(node.Operand);
			Parts[Parts.Count - 1] = Parts[Parts.Count - 1] with { Accessor = Parts[Parts.Count - 1].Accessor + "!" };
		}
	}

	public static PropertySelectorPath Resolve(SimpleLambdaExpressionSyntax selector)
	{
		var paramName = selector.Parameter.Identifier.ValueText;

		var visitor = new Visitor();
		var parts = visitor.Parts;

		var node = selector.ExpressionBody;
		if (node is not null)
		{
			visitor.Visit(node);
		}

		// TODO: PS0001 and PS0002
		return new(string.Concat(parts.Select(part => part.Accessor)), parts);
	}
}
