namespace Uno.Extensions.Navigation;

public interface IViewResolver
{
	ViewMap? FindByViewModel(Type? viewModelType);

	ViewMap? FindByView(Type? viewType);

	ViewMap? FindByData(Type? dataType);

	ViewMap? FindByResultData(Type? resultDataType);
}
