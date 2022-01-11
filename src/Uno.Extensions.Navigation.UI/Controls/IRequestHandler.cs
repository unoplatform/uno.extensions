namespace Uno.Extensions.Navigation.UI;

public interface IRequestHandler
{
	bool CanBind(FrameworkElement view);

	IRequestBinding? Bind(FrameworkElement view);
}

public interface IRequestBinding
{
	void Unbind();
}
