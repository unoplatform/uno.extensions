using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface IMessageBuilder
{
	MessageAxisValue this[MessageAxis axis] { get; set; }
}
