using System.Runtime.CompilerServices;
using Uno.Extensions.Markup.Internals;

namespace Uno.Extensions.Maui;

public static class MauiHostMarkupExtensions
{
	[MarkupExtension]
	public static MauiHost Source(this MauiHost host, Type type)
	{
		if (!type.IsSubclassOf(typeof(VisualElement)))
			throw new InvalidOperationException("The source Type must be of type VisualElement");

		host.Source = type;
		return host;
	}

	[MarkupExtension]
	public static MauiHost Source<TView>(this MauiHost host)
		where TView : VisualElement =>
		Source(host, typeof(TView));

	[MarkupExtension]
	public static MauiHost Source(this MauiHost host, Action<IDependencyPropertyBuilder<Type>> configureProperty)
	{
		DependencyPropertyBuilder<Type> instance = DependencyPropertyBuilder<Type>.Instance;
		configureProperty(instance);
		instance.SetBinding(host, MauiHost.SourceProperty, "Source");
		return host;
	}

	[MarkupExtension]
	public static MauiHost Source<TSource>(this MauiHost host, Func<TSource> propertyBinding, [CallerArgumentExpression("propertyBinding")]string? propertyBindingExpression = null) =>
		host.Source(x => x.Bind(propertyBinding, propertyBindingExpression));

	[MarkupExtension]
	public static MauiHost Source<TSource>(this MauiHost host, Func<TSource> propertyBinding, Func<TSource, Type> convertDelegate, [CallerArgumentExpression("propertyBinding")] string? propertyBindingExpression = null) =>
		host.Source(x => x.Bind(propertyBinding, propertyBindingExpression).Convert(convertDelegate));
}
