namespace Uno.Extensions.Navigation.Controls;

public interface IRequestHandler
{
	bool CanBind(FrameworkElement view);

	void Bind(FrameworkElement view);
}
