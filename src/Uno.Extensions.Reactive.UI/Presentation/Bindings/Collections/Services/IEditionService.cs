using System;
using System.Linq;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Facades.Differential;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

internal interface IEditionService
{
	/// <summary>
	/// Apply a collection changed issued by the view
	/// </summary>
	/// <param name="args">The change args.</param>
	void Update(Func<IDifferentialCollectionNode, IDifferentialCollectionNode> args);
}
