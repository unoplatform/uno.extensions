namespace Uno.Extensions.Navigation.Regions;

public sealed class NavigationRegion : IRegion
{
	private readonly ILogger _logger;

	public string? Name { get; private set; }

	public FrameworkElement? View { get; }

	private IServiceProvider? _services;
	private IRegion? _parent;
	private WeakReference<IRegion>? _previousParent;
	private bool _isRoot;
	private bool _isLoaded;
	public IRegion? Parent
	{
		get => _parent;
		private set
		{
			if (_parent is not null)
			{
				_parent.Children.Remove(this);
				_previousParent = new WeakReference<IRegion>(_parent);
			}
			_parent = value;
			if (_parent is not null)
			{
				_parent.Children.Add(this);
			}
		}
	}

	private INavigator? Navigator => this.Navigator();

  public IServiceProvider? Services
	{
		get
		{
			if (_services is null && Parent is not null)
			{

				_services = Parent?.Services?.CreateNavigationScope();
				if (_services is null)
				{
					return null;
				}

				_services.AddScopedInstance<IRegion>(this);
				var serviceFactory = _services.GetRequiredService<INavigatorFactory>();
#pragma warning disable CS8603 // Possible null reference return.
				_services.AddScopedInstance<INavigator>(() => serviceFactory.CreateService(this));
#pragma warning restore CS8603 // Possible null reference return.
			}

			return _services;
		}
	}

	public ICollection<IRegion> Children { get; } = new List<IRegion>();

	public NavigationRegion(ILogger logger, FrameworkElement? view = null, IServiceProvider? services = null)
	{
		_logger = logger;
		View = view;
		if (View is not null)
		{
			View.SetInstance(this);
			if (!View.IsLoaded)
			{
				if (_logger.IsEnabled(LogLevel.Trace))
				{
					_logger.LogTraceMessage($"(Name: {Name}) View is not loaded");
				}
				View.Loading += ViewLoading;
				View.Loaded += ViewLoaded;
			}
			else
			{
				if (_logger.IsEnabled(LogLevel.Trace))
				{
					_logger.LogTraceMessage($"(Name: {Name}) View is Loaded");
				}
				View.Unloaded += ViewUnloaded;
			}
		}
		else
		{
			if(_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"(Name: {Name}) View is null");
			}
		}

		if (services is not null)
		{
			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"(Name: {Name}) Services not null, so initialize root region");
			}
			InitializeRootRegion(services);
		}

		if (View is not null &&
			View.IsLoaded)
		{
			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"(Name: {Name}) View is already loaded");
			}

			_ = HandleLoading();
		}

	}
	public void Detach()
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"(Name: {Name}) Setting parent to null, to detach from region hierarchy");
		}

		this.Parent = null;
	}

	private void InitializeRootRegion(IServiceProvider services)
	{
		_isRoot = true;
		_services = services;
		_services.AddScopedInstance<IRegion>(this);
		var serviceFactory = _services.GetRequiredService<INavigatorFactory>();
		var navigator = serviceFactory.CreateService(this);
#pragma warning disable CS8603 // Possible null reference return.
		_services.AddScopedInstance<INavigator>(() => navigator);
#pragma warning restore CS8603 // Possible null reference return.
	}

	private async void ViewLoaded(object sender, RoutedEventArgs e)
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"(Name: {Name}) View is loaded");
		}
    
		await HandleLoading();
	}

#if WINDOWS_UWP || WINUI || NETSTANDARD
	private async void ViewLoading(FrameworkElement sender, object args)
#else
    private async void ViewLoading(DependencyObject sender, object args)
#endif
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"(Name: {Name}) View is loading");
		}

		await HandleLoading();
	}

	private void ViewUnloaded(object sender, RoutedEventArgs e)
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"(Name: {Name}) (Name: {Name}) View is unloaded");
		}

		if (View is null ||
			!_isLoaded)
		{
			return;
		}

		_isLoaded = false;

		View.Loading += ViewLoading;
		View.Loaded += ViewLoaded;
		View.Unloaded -= ViewUnloaded;

		Parent = null;
	}

	private Task HandleLoading()
	{
		if (View is null)
		{
			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"(Name: {Name}) View is null");
			}

			return Task.CompletedTask;
		}

		AssignParent();

		return View.IsLoaded ? HandleLoaded() : Task.CompletedTask;
	}

	private void AssignParent(IRegion? fallbackParent = null)
	{
		if (View is null || _isRoot || !View.GetAttached())
		{
			return;
		}

		if (Parent is null)
		{
			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"(Name: {Name}) Parent is null, so traverse visual tree looking for parent region");
			}

			var parent = View.FindParentRegion(out var routeName);
			Name = routeName;

			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"(Name: {Name}) Parent region found ({parent is not null}) with name ({Name})");
			}
      
			if (parent is not null)
			{
				Parent = parent;
			}
			else if (fallbackParent is not null)
			{
				if (_logger.IsEnabled(LogLevel.Trace))
				{
					_logger.LogTraceMessage($"(Name: {Name}) Cannot find parent in visual tree, using fallback parent to avoid orphaning during Hot Reload");
				}
				Parent = fallbackParent;
				return;
			}
		}

		if (Parent is null && !_isRoot && _services is null)
		{
			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"(Name: {Name}) No parent, and root region hasn't been created, so assume this region should be root");
			}


			var sp = View.FindServiceProvider();
			var services = sp?.CreateNavigationScope();
			if (services is null)
			{
				if (_logger.IsEnabled(LogLevel.Warning))
				{
					_logger.LogWarningMessage($"(Name: {Name}) Unable to find service provider for root navigator");
				}

				return;
			}

			InitializeRootRegion(services);

			var nav = this.Navigator();
			if (nav is not null)
			{
				var start = () => nav.NavigateRouteAsync(this, route: string.Empty);
				_ = services.Startup(start);
			}
		}
	}

	public void ReassignParent()
	{
		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"Reassigning parent (set parent to null and then call AssignParent to find new parent)");
		}

		if (View is null || _isRoot || !View.GetAttached())
		{
			if (_logger.IsEnabled(LogLevel.Trace))
			{
				_logger.LogTraceMessage($"(Name: {Name}) Cannot reassign parent: View is null ({View is null}), IsRoot ({_isRoot}), or not attached ({!(View?.GetAttached() ?? false)})");
			}
			return;
		}

		IRegion? fallbackParent = null;
		_previousParent?.TryGetTarget(out fallbackParent);

		AssignParent(fallbackParent);
	}

	private async Task HandleLoaded()
	{
		if (View is null || _isLoaded)
		{
			return;
		}
		_isLoaded = true;

		View.Loading -= ViewLoading;
		View.Loaded -= ViewLoaded;
		View.Unloaded += ViewUnloaded;

		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"(Name: {Name}) Forcing retrieval of navigator (will be created if not exists)");
		}

		// Force the lookup (and creation) of the navigator
		// This is required to intercept control event such as
		// navigating forward/backward on frame, or switching tabs
		var navigator = this.Navigator();

		if (navigator is not null)
		{
			foreach (var child in Children.OfType<NavigationRegion>())
			{
				if (child._isLoaded && child._services is null)
				{
					// This will force the setup of Services, which will
					// in turn force the creation of the navigator
					_ = child.Navigator();
				}
			}
		}
	}

	public async Task<string> GetStringAsync()
	{
		var sb = new StringBuilder();
		await PrintAllRegions(sb, this);
		return sb.ToString();
	}

	private static async Task PrintAllRegions(StringBuilder builder, IRegion region)
	{
		if (!string.IsNullOrWhiteSpace(region.Name))
		{
			builder.Append($@"{region.Name}");
		}

		if (region.View is not null)
		{
			builder.Append($@"({region.View.GetType().Name})-");
		}

		await region.View.EnsureLoaded();
		var nav = region.Navigator();
		if (nav is not null)
		{
			builder.Append($"{nav.ToString()}");
		}

		if (region.Children.Any())
		{
			builder.Append(" [");
		}

		foreach (var child in region.Children)
		{
			await PrintAllRegions(builder, child);
		}

		if (region.Children.Any())
		{
			builder.Append("]");
		}
	}
}
