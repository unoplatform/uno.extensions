using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

internal interface IBindable
{
	void OnPropertyChanged(string propertyName);
}
