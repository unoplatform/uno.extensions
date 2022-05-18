using System;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;

namespace Uno.Extensions.Reactive.Messaging;

/// <summary>
/// A message that can be send through <see cref="IMessenger"/> to indicate that a changed has been successfully made on an entity.
/// </summary>
/// <typeparam name="T">The type of the updated entity.</typeparam>
/// <param name="Change">The type of the change.</param>
/// <param name="Value">The updated entity.</param>
public record EntityMessage<T>(EntityChange Change, T Value);
