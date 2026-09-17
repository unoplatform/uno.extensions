using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// Spec 013 — MVUX mocking metadata emission (D11/D12).
///
/// Emits, on the model partial (opt-out via <c>[assembly: EnableFeedMocking(IsEnabled = false)]</c>):
///   - <c>[FeedDependency(member, OnParameter/OnFeed)]</c> classifying every feed member as
///     service-dependent input / derived / independent (read by the external mocking generator);
///   - <c>[CtorDependency(param, Eager=true)]</c> for constructor parameters dereferenced eagerly,
///     so the generated <c>Create(...)</c> can require them (R1 — would NRE under null-inject).
///
/// The analysis itself lives in <see cref="FeedMockingAnalysis"/>, shared with the mocking generator
/// so the two cannot disagree about what a model's inputs are.
///
/// There are deliberately NO per-feed swap hooks (D11): the runtime swap is reflection over the
/// model's <c>IHotSwapState&lt;T&gt;</c> members, reusing the hot-reload driver, fail-hard.
///
/// When the opt-out is present, <see cref="GenerateMockingMetadata"/> returns an empty string, so the
/// generated output is byte-identical (G5).
/// </summary>
internal partial class ViewModelGenTool_3
{
	private string GenerateMockingMetadata(INamedTypeSymbol model)
	{
		if (!_ctx.IsMockingEnabled())
		{
			return string.Empty; // opt-out → byte-identical output (G5)
		}

		// Scoped to this model rather than held on the generator: the analysis caches bound syntax trees,
		// which is worth it across one model's members but must not outlive the compilation it read.
		var analysis = new FeedMockingAnalysis(_ctx.Context.Compilation, IsFeedMember);

		var feedMembers = analysis.GetFeedMembers(model);
		var feedMemberNames = new HashSet<string>(feedMembers.Select(m => m.Name), StringComparer.Ordinal);
		var ctorParamNames = analysis.GetCtorParameterNames(model);
		var fieldToParam = analysis.BuildFieldToParamMap(model, ctorParamNames);

		var sb = new StringBuilder();

		// 1) Feed classification.
		foreach (var member in feedMembers)
		{
			var (kind, derivedFrom, services) = analysis.ClassifyFeedMember(member, feedMemberNames, ctorParamNames, fieldToParam, model);

			switch (kind)
			{
				case FeedKind.Derived:
					foreach (var feed in derivedFrom)
					{
						sb.Append($"\r\n[{NS.Config}.FeedDependency(\"{member.Name}\", OnFeed = \"{feed}\")]");
					}
					break;

				case FeedKind.ServiceDependent:
					foreach (var svc in services)
					{
						sb.Append($"\r\n[{NS.Config}.FeedDependency(\"{member.Name}\", OnParameter = \"{svc}\")]");
					}
					break;

				default:
					sb.Append($"\r\n[{NS.Config}.FeedDependency(\"{member.Name}\")]");
					break;
			}
		}

		// 2) Ctor instrumentation — eager parameter dereference (R1).
		var eager = analysis.FindEagerCtorParameters(model, ctorParamNames);
		foreach (var kvp in eager.OrderBy(k => k.Key, StringComparer.Ordinal))
		{
			var members = kvp.Value.Count > 0
				? $", Members = new[] {{ {string.Join(", ", kvp.Value.OrderBy(m => m, StringComparer.Ordinal).Select(m => $"\"{m}\""))} }}"
				: string.Empty;
			sb.Append($"\r\n[{NS.Config}.CtorDependency(\"{kvp.Key}\", Eager = true{members})]");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Emits the view-model mocking seam (spec 013, gated by opt-out). Commands have no
	/// <c>IHotSwapState&lt;T&gt;</c> backing, so the reflection swap (D11) cannot reach them: a dedicated
	/// public <c>__Mock_SetCommand</c> hook lets the external mocking generator override a command
	/// after construction. Fail-hard: an unknown command name throws (strict mocking, like D11).
	///
	/// Construction itself needs NO seam: the generated public constructors + the ambient
	/// <c>MockingService.Enable()</c> scope (D12) already produce a mockable <c>SourceContext</c>
	/// (the bit is captured on the context instance at creation, so a lazy first subscription after
	/// the scope is disposed still wraps).
	/// </summary>
	private string GenerateVmMockingSeam(IEnumerable<IMappedMember> members)
	{
		if (!_ctx.IsMockingEnabled())
		{
			return string.Empty; // opt-out → byte-identical output (G5)
		}

		var commands = members.OfType<CommandFromMethod>().ToList();
		if (commands.Count == 0)
		{
			return string.Empty;
		}

		var cases = commands
			.Select(c => $"case \"{c.Name}\": {c.Name} = command; break;")
			.JoinBy("\r\n");

		return $@"
			[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			public void __Mock_SetCommand(string name, {NS.Reactive}.IAsyncCommand command)
			{{
				switch (name)
				{{
					{cases}
					default: throw new global::System.ArgumentException($""No mockable command '{{name}}' on this view model."", nameof(name));
				}}
			}}";
	}

	private bool IsFeedMember(ISymbol member)
	{
		var type = member switch
		{
			IPropertySymbol p => p.Type,
			IFieldSymbol f => f.Type,
			_ => null,
		};
		return type is not null && (_ctx.IsFeed(type) || _ctx.IsListFeed(type) || _ctx.IsFeedOfList(type));
	}
}
