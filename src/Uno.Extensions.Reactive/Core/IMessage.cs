using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface IMessage
{
	IMessageEntry Previous { get; }
	IMessageEntry Current { get; }
	IReadOnlyCollection<MessageAxis> Changes { get; }
}
