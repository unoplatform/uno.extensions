using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Generator;

internal interface IMappedMember
{
	public string GetDeclaration();

	public string? GetInitialization();
}
