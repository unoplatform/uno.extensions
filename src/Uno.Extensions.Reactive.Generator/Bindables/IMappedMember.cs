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

	/// <summary>
	/// Gets the type of the mock override property for this member in the mocks bundle,
	/// or null if the member is not mockable (i.e. it forwards directly to the model).
	/// </summary>
	/// <returns>E.g. 'global::Uno.Extensions.Reactive.IListFeed&lt;MyItem&gt;'</returns>
	public string? GetMockPropertyType();

	/// <summary>
	/// Gets the code to initialize the member in the mock constructor, from the mock override
	/// when set, falling back to a feed pinned in the Undefined state (or an idle command).
	/// Null when the member is not mockable. Unlike <see cref="GetInitialization"/>, this code
	/// must not reference the model.
	/// </summary>
	/// <param name="mocks">Name of the non-null mocks-bundle variable.</param>
	public string? GetMockInitialization(string mocks);
}
