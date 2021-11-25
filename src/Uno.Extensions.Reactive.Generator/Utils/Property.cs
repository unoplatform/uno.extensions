using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;

namespace Uno.Extensions.Reactive.Generator;

public record Property(Accessibility Accessibility, string Type, string Name)
{
	public Property(Accessibility accessibility, ITypeSymbol type, string name)
		: this(accessibility, type.ToString(), name)
	{
	}

	public Property(ITypeSymbol type, string name)
		: this(type.DeclaredAccessibility, type.ToString(), name)
	{
	}

	public bool? HasGetter { get; init; }
	public Accessibility? GetterAccessibility { get; init; }
	public string? Getter { get; init; }
	public bool? HasSetter { get; init; }
	public Accessibility? SetterAccessibility { get; init; }
	public string? Setter { get; init; }
	public bool IsInit { get; init; }

	public static string Generate(IPropertySymbol property, string? getter, string? setter)
	{
		var sb = new IndentedStringBuilder();

		sb.AppendLine($"{property.GetAccessibilityAsCSharpCodeString()} {property.Type} {property.Name}");
		sb.AppendLine();
		using (sb.Block())
		{
			if (getter is not null && (property.GetMethod?.IsAccessible() ?? false))
			{
				if (property.GetMethod?.DeclaredAccessibility != property.DeclaredAccessibility)
				{
					sb.Append(property.GetMethod?.GetAccessibilityAsCSharpCodeString());
					sb.Append(" ");
				}

				sb.AppendLine($"get => {getter};");
				sb.AppendLine();
			}

			if (setter is not null && (property.SetMethod?.IsAccessible() ?? false))
			{
				if (property.SetMethod?.DeclaredAccessibility != property.DeclaredAccessibility)
				{
					sb.Append(property.GetMethod?.GetAccessibilityAsCSharpCodeString());
					sb.Append(" ");
				}

				sb.AppendLine($"set => {setter};");
				sb.AppendLine();
			}
		}

		return sb.ToString();
	}

	public static Property FromProperty(IPropertySymbol property)
		=> new(property.Type, property.Name)
		{
			Accessibility = property.DeclaredAccessibility,
			HasGetter = property.GetMethod?.IsAccessible() ?? false,
			HasSetter = property.SetMethod?.IsAccessible() ?? false
		};

	public string Generate()
		=> Generate(Accessibility, Type, Name, HasGetter, GetterAccessibility, Getter, HasSetter, SetterAccessibility, Setter, IsInit);

	public static string Generate(
		Accessibility accessibility,
		string type,
		string name,
		bool? hasGetter, Accessibility? getterAccessibility, string? getter,
		bool? hasSetter, Accessibility? setterAccessibility, string? setter, bool init = false)
	{
		var sb = new IndentedStringBuilder();

		sb.AppendLine($"{accessibility.ToCSharpCodeString()} {type} {name}");
		sb.AppendLine();
		using (sb.Block())
		{
			if (hasGetter is null or true && getter is not null)
			{
				if (getterAccessibility is {} ga && ga != accessibility)
				{
					sb.Append(ga.ToCSharpCodeString());
					sb.Append(" ");
				}

				sb.AppendLine($"get => {getter};");
				sb.AppendLine();
			}

			if (hasSetter is null or true && setter is not null)
			{
				if (setterAccessibility is { } sa && sa != accessibility)
				{
					sb.Append(sa.ToCSharpCodeString());
					sb.Append(" ");
				}

				sb.AppendLine(init ? "init" : "set");
				sb.AppendLine($" => {setter};");
				sb.AppendLine();
			}
		}

		return sb.ToString();
	}

	/// <inheritdoc />
	public override string ToString()
		=> Generate();

	public static implicit operator string(Property property)
		=> property.ToString();
}
