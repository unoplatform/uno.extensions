namespace Uno.Extensions.Navigation.Regions;

public interface IRegion
{
	string? Name { get; }

	FrameworkElement? View { get; }

	IServiceProvider? Services { get; }

	IRegion? Parent { get; }

	ICollection<IRegion> Children { get; }

	void ReassignParent();

	void Detach();
}
