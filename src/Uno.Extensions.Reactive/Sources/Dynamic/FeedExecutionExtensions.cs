using System;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Reactive.Sources.Pagination;

namespace Uno.Extensions.Reactive.Sources;

internal static class FeedExecutionExtensions
{
	public static void EnableRefresh(this FeedExecution execution)
		=> execution.Session.GetShared(nameof(RefreshDependency), static (session, _, __) => new RefreshDependency(session), Unit.Default).Enable();

	public static ValueTask<IImmutableList<TItem>> GetPaginated<TItem>(this FeedExecution execution, Func<PaginationBuilder<TItem>, PaginationConfiguration<TItem>> configure)
	{
		var dependency = execution.Session.GetShared(
			nameof(PaginationDependency<TItem>),
			static (session, key, _) => new PaginationDependency<TItem>(session, key),
			Unit.Default);

		return dependency.GetItems(execution, static (b, c) => c(b), configure);
	}

	public static ValueTask<IImmutableList<TItem>> GetPaginated<TItem>(this FeedExecution execution, string identifier, Func<PaginationBuilder<TItem>, PaginationConfiguration<TItem>> configure)
	{
		var dependency = execution.Session.GetShared(
			(typeof(PaginationDependency<TItem>), identifier),
			static (session, key, _) => new PaginationDependency<TItem>(session, key.identifier),
			Unit.Default);

		return dependency.GetItems(execution, static (b, c) => c(b), configure);
	}

	public static ValueTask<IImmutableList<TItem>> GetPaginated<TItem, TArgs>(this FeedExecution execution, Func<PaginationBuilder<TItem>, TArgs, PaginationConfiguration<TItem>> configure, TArgs args)
	{
		var dependency = execution.Session.GetShared(
			nameof(PaginationDependency<TItem>),
			static (session, key, _) => new PaginationDependency<TItem>(session, key),
			Unit.Default);

		return dependency.GetItems(execution, configure, args);
	}

	public static ValueTask<IImmutableList<TItem>> GetPaginated<TItem, TArgs>(this FeedExecution execution, string identifier, Func<PaginationBuilder<TItem>, TArgs, PaginationConfiguration<TItem>> configure, TArgs args)
	{
		var dependency = execution.Session.GetShared(
			(typeof(PaginationDependency<TItem>), identifier),
			static (session, key, _) => new PaginationDependency<TItem>(session, key.identifier),
			Unit.Default);

		return dependency.GetItems(execution, configure, args);
	}
}
