using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface IMessageBuilder<in T> : IMessageBuilder
{
}
