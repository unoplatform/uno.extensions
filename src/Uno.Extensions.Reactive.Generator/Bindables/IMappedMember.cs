using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A member of the user's ViewModel that is being mapped into the BindableVM
/// </summary>
internal interface IMappedMember
{
	public string Name { get; }

	public string GetDeclaration();

	public string? GetInitialization();
}
