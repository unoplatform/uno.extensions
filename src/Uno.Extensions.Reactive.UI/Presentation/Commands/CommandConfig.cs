using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

/// <summary>
/// The raw configuration of a command.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)] // This should be used only by code gen
public record struct CommandConfig
{
	private CommandParametersCoercingStrategy? _parametersCoercer;

	/// <summary>
	/// Command parameter provider (if the parameter is not coming from the view).
	/// </summary>
	/// <remarks>
	/// By default this **overrides** the `CommandParameter` defined in the view,
	/// i.e. the `CommandParameter` will be completely ignored for <see cref="CommandConfig.CanExecute"/> and <see cref="CommandConfig.Execute"/> methods.
	/// You can use the <see cref="ParametersCoercing"/> to behave differently.
	/// </remarks>
	public Func<SourceContext, IAsyncEnumerable<IMessage>>? Parameter { get; set; }

	/// <summary>
	/// **If <see cref="Parameter"/> is defined**, the strategy to use to coerce the parameters coming from view the and <see cref="Parameter"/>.
	/// </summary>
	/// <remarks>
	/// If not explicitly set, the default coercion will be <see cref="CommandParametersCoercingStrategy.UseExternalParameterOnly"/> if <see cref="Parameter"/> has been set,
	/// <see cref="CommandParametersCoercingStrategy.UseViewParameterOnly"/> otherwise.
	/// </remarks>
	public CommandParametersCoercingStrategy ParametersCoercing
	{
		get => _parametersCoercer ?? (Parameter is null ? CommandParametersCoercingStrategy.UseViewParameterOnly : CommandParametersCoercingStrategy.UseExternalParameterOnly);
		set => _parametersCoercer = value;
	}

	/// <summary>
	/// Can execute delegate.
	/// </summary>
	public Predicate<object?>? CanExecute { get; set; }

	/// <summary>
	/// The action to execute.
	/// </summary>
	public AsyncAction<object?> Execute { get; set; }
}
