using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources.Pagination;

internal record struct PaginationConfiguration<TItem>(Func<CancellationToken, IPageEnumerator<TItem>> GetEnumerator);
