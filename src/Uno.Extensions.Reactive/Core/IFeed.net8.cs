using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

[AsyncMethodBuilder(typeof(FeedMethodBuilder<>))]
partial interface IFeed<T>
{
}
