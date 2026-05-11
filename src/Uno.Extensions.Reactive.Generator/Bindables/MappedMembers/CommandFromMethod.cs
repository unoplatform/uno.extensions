using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;
using Uno.Extensions.Reactive.Config;

namespace Uno.Extensions.Reactive.Generator;

internal partial record CommandFromMethod : IMappedMember
{
	private readonly ImmutableArray<CommandParameter> _parameters;

	public static bool TryCreate(INamedTypeSymbol type, IMethodSymbol method, BindableGenerationContext context, [NotNullWhen(true)] out CommandFromMethod? generator)
	{
		if (method is not
			{
				MethodKind: MethodKind.Ordinary,
				IsImplicitlyDeclared: false,
				DeclaredAccessibility: Accessibility.Public,
			})
		{
			generator = null;
			return false;
		}

		var isCommandEnabled = method.FindAttributeValue<bool>(context.CommandAttribute, ctorPosition: 0).value
			?? type.FindAttributeValue<bool>(context.ImplicitCommandsAttribute, ctorPosition: 0).value
			?? type.ContainingAssembly.FindAttributeValue<bool>(context.ImplicitCommandsAttribute, ctorPosition: 0).value
			?? ImplicitCommandsAttribute.DefaultValue;
		if (!isCommandEnabled)
		{
			generator = null;
			return false;
		}

		var parameters = ResolveParameters(type, method, context);
		if (parameters.Count(param => param.IsCommandParameter) is 0 or 1
			&& parameters.Count(param => param.IsCancellation) is 0 or 1)
		{
			generator = new CommandFromMethod(type, method, parameters, context);
			return true;
		}

		generator = null;
		return false;
	}

	private CommandFromMethod(INamedTypeSymbol type, IMethodSymbol method, ImmutableArray<CommandParameter> parameters, BindableGenerationContext context)
	{
		_parameters = parameters;
		Type = type;
		Method = method;
		Context = context;
	}

	/// <inheritdoc />
	public string Name => Method.Name;

	public INamedTypeSymbol Type { get; }

	public IMethodSymbol Method { get; }

	public BindableGenerationContext Context { get; }

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{Method.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IAsyncCommand {Name} {{ get; private set; }}";

	/// <inheritdoc />
	public string GetInitialization()
	{
		var configs = new List<CommandConfigGenerator>();
		var parameters = _parameters;
		var feedParameters = parameters.Where(param => param.IsFeedParameter).ToArray();
		var cancellationParameters = parameters.Where(param => param.IsCancellation).ToArray();
		var viewParameter = parameters.SingleOrDefault(p => p.IsCommandParameter);

		if (parameters.IsDefaultOrEmpty || parameters.All(p => p.IsCancellation))
		{
			configs.Add(new CommandConfigGenerator(this)
			{
				DeconstructParameters = (args, ct) => GetDeconstruct(args, ct),
			});
		}
		else if (feedParameters.Any())
		{
			// Some parameters are externally injected by the VM using Feeds

			var sourceFeed = feedParameters is { Length: 1 }
				? GetFeed(feedParameters[0])
				: $"{NS.Reactive}.Feed.Combine(\r\n\t{feedParameters.Select(GetFeed).JoinBy(",\r\n").Align(1)})";

			string GetFeed(CommandParameter parameter)
			{
				var feed = $"{N.Ctor.Model}.{parameter.FeedProperty!.Name}";
				if (parameter.IsListFeedParameter)
				{
					feed = $"{NS.Reactive}.ListFeed.AsFeed({feed})";
				}
				return feed;
			}

			if (viewParameter is not null)
			{
				// If we have a mix of view and external parameters, we have to coerce them.
				configs.Add(new CommandConfigGenerator(this)
				{
					ExternalParameter = $"ctx => ctx.GetOrCreateSource({sourceFeed})",
					ParametersCoercer = $"{NS.Commands}.CommandParametersCoercingStrategy.UseBoth((viewParameter, feedParameter) => (viewParameter is {viewParameter.Symbol.Type.ToFullString()} vp ? vp : default, feedParameter is {GetTypeOrTuple(feedParameters)} fp ? fp : default))",
					ParameterType = $"global::System.ValueTuple<{viewParameter.Symbol.Type.ToFullString()}, {GetTypeOrTuple(feedParameters)}>",
					DeconstructParameters = (args, ct) => GetDeconstruct($"{args}.Item2", ct, viewArg: $"{args}.Item1"),
					CanExecute = CheckForNulls(new[] { viewParameter }) is { } check ? args => check($"{args}.Item1") : null,
				});
			}
			else
			{
				// All parameters are external, we can then allow 2 way to invoke command:
				//	 1. Using external parameters only
				//	 2. Providing all arguments in the CommandParameter (except CT)

				configs.Add(new CommandConfigGenerator(this)
				{
					// Note: We don't CheckForNull with external parameters only, they are validated by the SubCommand
					ExternalParameter = $"ctx => ctx.GetOrCreateSource({sourceFeed})",
					ParametersCoercer = $"{NS.Commands}.CommandParametersCoercingStrategy.AllowOnlyExternalParameter",
					ParameterType = GetTypeOrTuple(feedParameters),
					DeconstructParameters = (args, ct) => GetDeconstruct(args, ct),
				});

				configs.Add(new CommandConfigGenerator(this)
				{
					ParameterType = GetTypeOrTuple(parameters.Where(p => !p.IsCancellation)),
					DeconstructParameters = (args, ct) => GetDeconstruct(args, ct),
					CanExecute = CheckForNulls(parameters.Where(p => !p.IsCancellation))
				});
			}
		}
		else
		{
			// The parameters must be sent using CommandParameter

			configs.Add(new CommandConfigGenerator(this)
			{
				ParameterType = GetTypeOrTuple(parameters.Where(p => !p.IsCancellation)),
				DeconstructParameters = (args, ct) => GetDeconstruct(args, ct),
				CanExecute = CheckForNulls(parameters.Where(p => !p.IsCancellation))
			});
		}

		string GetTypeOrTuple(IEnumerable<CommandParameter> parameters)
			=> parameters.Count() is 1
				? parameters.First().Symbol.Type.ToFullString()
				: $"global::System.ValueTuple<{parameters.Select(p => p.Symbol.Type.ToFullString()).JoinBy(", ")}>";

		Func<string, string>? CheckForNulls(IEnumerable<CommandParameter> parameters)
		{
			if (parameters.Count() is 1)
			{
				if (parameters.First().Symbol is { Type.IsValueType: false, NullableAnnotation: NullableAnnotation.NotAnnotated })
				{
					return args => $"{args} is not null";
				}
			}
			else
			{
				if (parameters.Any(p => p.Symbol.NullableAnnotation is NullableAnnotation.NotAnnotated))
				{
					return args => parameters
						.Select((p, i) => p.Symbol is { Type.IsValueType: false, NullableAnnotation: NullableAnnotation.NotAnnotated }
							? $"{args}.Item{i+1} is not null"
							: null)
						.Where(s => s is not null)
						.JoinBy(" && ");
				}
			}

			return null;
		}

		string GetDeconstruct(string args, string ct, string? viewArg = null)
		{
			var result = new StringBuilder();
			if (viewParameter is not null)
			{
				result.AppendLine($"var {viewParameter.Symbol.Name} = {viewArg ?? args};");
			}

			if (feedParameters.Length is 1)
			{
				result.AppendLine($"var {feedParameters.First().Symbol.Name} = {args};");
			}
			else
			{
				for (var i = 0; i < feedParameters.Length; i++)
				{
					result.AppendLine($"var {feedParameters[i].Symbol.Name} = {args}.Item{i+1};");
				}
			}

			foreach (var ctParam in cancellationParameters)
			{
				result.AppendLine($"var {ctParam.Symbol.Name} = {ct};"); // we are ignoring any CT provided by caller
			}

			return result.ToString();
		};

		return @$"{Name} ??= new {NS.Commands}.AsyncCommand(
					nameof({Name}),
					new {NS.Commands}.CommandConfig[]
					{{
						{configs.Select(config => config.ToString()).JoinBy(",\r\n").Align(6)}
					}},
					{NS.Reactive}.Command.DefaultErrorHandler,
					{N.Ctor.Ctx}
				);";
	}

	private static ImmutableArray<CommandParameter> ResolveParameters(INamedTypeSymbol type, IMethodSymbol method, BindableGenerationContext ctx)
	{
		var isImplicitParametersEnabled = type.FindAttributeValue<bool>(ctx.ImplicitCommandParametersAttribute, ctorPosition: 0).value
			?? type.ContainingAssembly.FindAttributeValue<bool>(ctx.ImplicitCommandParametersAttribute, ctorPosition: 0).value
			?? ImplicitFeedCommandParametersAttribute.DefaultValue;
		return method.Parameters.Select(Resolve).ToImmutableArray();

		CommandParameter Resolve(IParameterSymbol parameter)
		{
			if (SymbolEqualityComparer.Default.Equals(parameter.Type, ctx.CancellationToken))
			{
				return new CommandParameter(parameter, IsCancellation: true);
			}

			var propertyAttr = parameter.FindAttributeValue(ctx.CommandParameterAttribute, ctorPosition: 0);
			if (propertyAttr is { isDefined: true } || isImplicitParametersEnabled)
			{
				var property = type.FindProperty(propertyAttr.value ?? parameter.Name, allowBaseTypes: true, comparison: StringComparison.OrdinalIgnoreCase);
				if (propertyAttr.isDefined)
				{
					if (property is null)
					{
						ctx.Context.ReportDiagnostic(Rules.FEED2001.GetDiagnostic(type, method, parameter));
					}
					else if (!ctx.IsFeed(property.Type) && !ctx.IsListFeed(property.Type))
					{
						ctx.Context.ReportDiagnostic(Rules.FEED2002.GetDiagnostic(type, method, parameter));
					}
				}

				if (property is not null
					&& ctx.IsFeed(property.Type, out var valueType)
					&& SymbolEqualityComparer.Default.Equals(parameter.Type, valueType))
				{
					return new CommandParameter(parameter, property);
				}

				if (property is not null
					&& ctx.IsListFeed(property.Type, out var itemType)
					&& SymbolEqualityComparer.Default.Equals(parameter.Type, ctx.IImmutableList.Construct(itemType)))
				{
					return new CommandParameter(parameter, property) { IsListFeedParameter = true };
				}
			}

			return new CommandParameter(parameter);
		}
	}
}
