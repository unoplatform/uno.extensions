using System;
using System.Linq;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// Tag interface for a feed that can be refreshed (cf. <see cref="RefreshToken"/>).
/// </summary>
internal interface IRefreshableSource
{
}

/// <summary>
/// Tag interface for a feed that can be refreshed (cf. <see cref="PageToken"/>).
/// </summary>
internal interface IPaginatedSource
{
}
