using System;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Sources.Pagination;

internal record struct PaginationConfiguration<TItem>(Func<CancellationToken, IPageEnumerator<TItem>> GetEnumerator);
