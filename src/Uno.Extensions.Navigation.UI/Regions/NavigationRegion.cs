namespace Uno.Extensions.Navigation.Regions;

public sealed class NavigationRegion : IRegion
{
    public string? Name { get; private set; }

    public FrameworkElement? View { get; }

    private IServiceProvider? _services;
    private IRegion? _parent;
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
            }
            _parent = value;
            if (_parent is not null)
            {
                _parent.Children.Add(this);
            }
        }
    }

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

	public NavigationRegion(FrameworkElement? view = null, IServiceProvider? services = null)
	{
		View = view;
		if (View is not null)
		{
			View.SetInstance(this);
			if (!View.IsLoaded)
			{
				View.Loading += ViewLoading;
				View.Loaded += ViewLoaded;
			}
			else
			{
				View.Unloaded += ViewUnloaded;
			}
		}

		if (services is not null)
		{
			InitializeRootRegion(services);
		}

		if (View is not null &&
			View.IsLoaded)
		{
			_ = HandleLoading();
		}

	}
	public void Detach()
	{
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
        await HandleLoading();
    }

#if WINDOWS_UWP || WINUI || NETSTANDARD
    private async void ViewLoading(FrameworkElement sender, object args)
#else
    private async void ViewLoading(DependencyObject sender, object args)
#endif
    {
        await HandleLoading();
    }

    private void ViewUnloaded(object sender, RoutedEventArgs e)
    {
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
            return Task.CompletedTask;
        }

        AssignParent();

        return View.IsLoaded ? HandleLoaded() : Task.CompletedTask;
    }

    private void AssignParent()
    {
        if (View is null || _isRoot || !View.GetAttached())
        {
            return;
        }

        if (Parent is null)
        {
            var parent = View.FindParentRegion(out var routeName);
            Name = routeName;
            if (parent is not null)
            {
                Parent = parent;
            }
        }

		if(Parent is null && !_isRoot && _services is null)
		{
			var sp = View.FindServiceProvider();
			var services = sp?.CreateNavigationScope();
			if (services is null)
			{
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
        Parent = null;
        AssignParent();
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

        // Force the lookup (and creation) of the navigator
        // This is required to intercept control event such as
        // navigating forward/backward on frame, or switching tabs
		var navigator = this.Navigator();

		if(navigator is not null)
		{
			foreach (var child in Children.OfType<NavigationRegion>())
			{
				if(child._isLoaded && child._services is null)
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
