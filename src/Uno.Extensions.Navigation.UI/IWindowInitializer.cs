namespace Uno.Extensions.Navigation;

[EditorBrowsable(EditorBrowsableState.Never)]
internal interface IWindowInitializer
{
	ValueTask InitializeWindowAsync(Window window);
}
