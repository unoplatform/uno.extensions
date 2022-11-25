﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

/// <summary>
/// An input from the view.
/// </summary>
/// <typeparam name="T">Type of the value of the input.</typeparam>
public interface IInput<T> : IState<T>
{
	/// <summary>
	/// The name of bindable property.
	/// </summary>
	public string PropertyName { get; }
}
