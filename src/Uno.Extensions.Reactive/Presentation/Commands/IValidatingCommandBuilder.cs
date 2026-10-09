using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Commands;

/// <summary>
/// A command builder that supports a validation step (cf. CommandBuilderExtensions.Validation).
/// </summary>
/// <remarks>
/// This works with object as the public builder interfaces are covariant
/// (e.g. an ICommandBuilder&lt;object&gt; might be a CommandBuilder&lt;string&gt;).
/// </remarks>
internal interface IValidatingCommandBuilder
{
	/// <summary>
	/// Adds a validation of the parameter, run on each execution of the command, before the action.
	/// If a validation has already been configured, it is replaced.
	/// </summary>
	/// <returns>The <see cref="IConditionalCommandBuilder{T}"/> to complete the configuration of the command.</returns>
	object Validation(Func<object?, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator);
}
