using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A member of the user's ViewModel that is being mapped into the BindableVM
/// </summary>
internal interface IMappedMember
{
	public string Name { get; }

	/// <summary>
	/// Gets the code to declare the backing field needed for the member, if any.
	/// </summary>
	/// <returns>E.g. 'private string _myField;'</returns>
	public string? GetBackingField();

	/// <summary>
	/// Get the code to declare the member.
	/// </summary>
	/// <returns>E.g. 'public string MyMember => _myField;'</returns>
	public string GetDeclaration();

	/// <summary>
	/// Gets the code need to initialize the member in constructor, is needed.
	/// </summary>
	/// <returns>E.g. '_myField = "Hello world";'</returns>
	public string? GetInitialization();
}
