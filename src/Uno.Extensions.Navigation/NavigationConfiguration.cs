namespace Uno.Extensions.Navigation;

/// <summary>
/// Options to adjust the behaviour of Navigation
/// </summary>
public record NavigationConfiguration
{
	/// <summary>
	/// The type to use to override the default route resolver
	/// </summary>
	public Type? RouteResolver { get; init; }

	/// <summary>
	/// Whether to update the address bar during navigation (WASM)
	/// </summary>
	public bool? AddressBarUpdateEnabled { get; init; }

	/// <summary>
	/// Whether to support the native back button (WASM)
	/// </summary>
	public bool? UseNativeBackButton { get; init; }
}
