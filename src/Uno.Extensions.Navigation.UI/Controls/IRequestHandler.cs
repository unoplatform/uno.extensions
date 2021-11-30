namespace Uno.Extensions.Navigation.UI;

public interface IRequestHandler
{
	bool CanBind(FrameworkElement view);

	void Bind(FrameworkElement view);
}
