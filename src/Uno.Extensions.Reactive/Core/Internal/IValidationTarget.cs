using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A state on which validation results can be published, no matter the type of its value (e.g. by commands, cf. CommandBuilderExtensions.Validation).
/// </summary>
internal interface IValidationTarget
{
	/// <summary>
	/// Publishes validation results on the <see cref="MessageAxis.Validation"/> of this state.
	/// </summary>
	/// <param name="results">The results to publish (empty to clear).</param>
	/// <param name="ct">A cancellation token: the results are not published if already cancelled, and the wait for the publication is aborted when cancelled.</param>
	ValueTask PublishValidationAsync(IImmutableList<ValidationResult> results, CancellationToken ct);
}
