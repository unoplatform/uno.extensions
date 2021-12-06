namespace Uno.Extensions.Navigation;

public interface IViewRegistry
{
	IViewRegistry Register(ViewMap view);

	IViewRegistry Register<TData>(ViewMap<TData> route)
		where TData : class;

	IViewRegistry Register<TData, TResultData>(ViewMap<TData, TResultData> route)
		where TData : class
		where TResultData : class;
}
