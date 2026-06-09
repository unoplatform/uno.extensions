namespace Uno.Extensions.Navigation.Regions;

public sealed class NavigationRegion : IRegion
{
	private readonly ILogger _logger;

	public string? Name { get; private set; }

	public FrameworkElement? View { get; }

	private IServiceProvider? _services;
	private IRegion? _parent;
	private bool _isRoot;
	private bool _isLoaded;
	private bool _wasUnloaded;
	// Set by FrameNavigator before Children.Clear() to indicate this region is being
	// unloaded due to navigation (not Hot Reload). When true, HandleLoaded skips the
	// HR re-cascade because the parent navigator will cascade the correct route itself.
	internal bool _suppressReCascadeOnReload;
	// Set by NavigationVisibilityUpdateHandler.RestoreState when this region was created
	// by HR's ReplaceViewInstance as a replacement for a region that had active navigation.
	// Treated as equivalent to _wasUnloaded in HandleLoaded's re-cascade check.
	private bool _replacedByHotReload;

	// When the view is Loaded but its visual ancestry up to the navigation root is not yet
	// connected (async tree construction, or a hot-reload view-swap that grafts the new subtree
	// before its ancestry is linked), AssignParent resolves neither a parent region nor a root
	// service provider. Committing to a loaded state then permanently orphans the region — a blank
	// screen. Instead we watch LayoutUpdated (the framework's "element has settled" signal, quiet at
	// idle — see FrameworkElementExtensions.EnsureElementLoaded) and re-attempt until the root
	// becomes reachable. _resolveWatching tracks the subscription; _resolveAttempts bounds the watch
	// so a region genuinely detached from any navigation root gives up (and logs) once.
	private const int MaxResolveAttempts = 30;
	private bool _resolveWatching;
	private int _resolveAttempts;

	// A region is fully resolved once it is the root or has a parent (either of which yields Services).
	// While it is loaded but none of those hold, its place in the navigation tree is not established yet.
	private bool IsLoadedButUnresolved =>
		View is { IsLoaded: true } && !_isRoot && Parent is null && _services is null;
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

		// Store root region reference for C# hot-reload route refresh. After
		// NavigationRouteUpdateHandler rebuilds the resolver with newly registered
		// routes, it walks down from this root to find navigators that need a
		// default-route re-cascade. Mirrors the resolver wire-up in
		// ServiceCollectionExtensions.AddNavigation.
		if (_services.GetService<NavigationRouteContext>() is { } ctx)
		{
			ctx.RootRegion = this;
		}
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
		_wasUnloaded = true;

		View.Loading += ViewLoading;
		View.Loaded += ViewLoaded;
		View.Unloaded -= ViewUnloaded;

		Parent = null;

		// Drop any pending resolve watch and reset the budget so a later re-load that re-orphans gets a
		// fresh allowance rather than inheriting a spent counter.
		StopResolveWatch();
		_resolveAttempts = 0;
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

	private void AssignParent()
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
				// Transient during async tree construction / a hot-reload view-swap: the ancestry up to
				// the navigation root is not connected yet. HandleLoaded watches for the tree to settle
				// and re-attempts; a terminal Warning is logged only if that watch is exhausted
				// (OnResolveLayoutUpdated), so this is Trace, not Warning, to avoid per-attempt spam.
				if (_logger.IsEnabled(LogLevel.Trace))
				{
					_logger.LogTraceMessage($"(Name: {Name}) Root service provider not reachable yet; will re-attempt once the visual tree settles");
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

		Parent = null;
		AssignParent();
	}

	/// <summary>
	/// Marks all descendant NavigationRegions so that when they reload after being
	/// unloaded by navigation (e.g., Children.Clear()), they do not spuriously
	/// re-cascade the parent route. This prevents overriding the correct child
	/// route that the parent navigator restores via AdjustRequestForChildNavigation.
	/// </summary>
	internal static void SuppressReCascadeOnDescendants(IRegion region)
	{
		foreach (var child in region.Children)
		{
			if (child is NavigationRegion navRegion)
			{
				navRegion._suppressReCascadeOnReload = true;
			}
			SuppressReCascadeOnDescendants(child);
		}
	}

	/// <summary>
	/// Called by <see cref="NavigationVisibilityUpdateHandler.RestoreState"/> when this
	/// region was created by hot-reload's <c>ReplaceViewInstance</c> as a replacement
	/// for a region that had active navigation. Causes <see cref="HandleLoaded"/> to
	/// re-cascade the parent route, re-injecting the ViewModel on the new page instance.
	/// </summary>
	internal void MarkReplacedByHotReload()
	{
		_replacedByHotReload = true;
	}

	private async Task HandleLoaded()
	{
		if (View is null || _isLoaded)
		{
			return;
		}

		// The view is connected to the live tree, but if the navigation root is not yet reachable do NOT
		// commit to a loaded/orphaned state. Keep the load-event subscriptions (so a re-parent recovers
		// via ViewLoaded) and watch LayoutUpdated to re-attempt once the tree settles. Committing here is
		// what previously produced the blank screen.
		if (IsLoadedButUnresolved)
		{
			StartResolveWatch();
			return;
		}

		// Resolution succeeded (root or parent found) — stop any pending resolve watch and commit.
		StopResolveWatch();
		_resolveAttempts = 0;

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

			// If the parent already has an active route (e.g., after XAML HR
			// recreated this region), re-trigger the route cascade from the parent
			// so this navigator receives its initial navigation via the normal flow.
			// Only applies on re-load after unload (HR scenario), not first-time load.
			// Skip if this region was unloaded due to navigation (not HR) — the parent
			// navigator (e.g., FrameNavigator) will cascade the correct route itself
			// via AdjustRequestForChildNavigation / NavigateChildRegions.
			if ((_wasUnloaded || _replacedByHotReload) && !_suppressReCascadeOnReload && Parent is not null && navigator.Route is null)
			{
				var parentNav = Parent.Navigator();
				if (parentNav?.Route is { Base.Length: > 0 })
				{
					if (_logger.IsEnabled(LogLevel.Trace))
					{
						_logger.LogTraceMessage($"(Name: {Name}) Parent already has route '{parentNav.Route}', re-cascading");
					}

					var request = new NavigationRequest(parentNav, parentNav.Route.AsInternal());
					_ = parentNav.NavigateAsync(request);
				}
			}
			_suppressReCascadeOnReload = false;
			_replacedByHotReload = false;
		}
	}

	// Watches for the visual tree to settle so an unresolved-but-loaded region can re-attempt to find its
	// navigation root. LayoutUpdated is the framework's "element settled" signal and is quiet at idle, so
	// this is event-driven, not a poll — EnsureElementLoaded relies on the same signal because Loaded
	// alone is unreliable on WASM. Loaded/Loading stay subscribed too, so a re-parent recovers via ViewLoaded.
	private void StartResolveWatch()
	{
		if (_resolveWatching || View is null)
		{
			return;
		}

		if (_logger.IsEnabled(LogLevel.Trace))
		{
			_logger.LogTraceMessage($"(Name: {Name}) Loaded but navigation root not reachable; watching for the visual tree to settle");
		}

		_resolveWatching = true;
		View.LayoutUpdated += OnResolveLayoutUpdated;
	}

	private void StopResolveWatch()
	{
		if (!_resolveWatching || View is null)
		{
			return;
		}

		_resolveWatching = false;
		View.LayoutUpdated -= OnResolveLayoutUpdated;
	}

	private void OnResolveLayoutUpdated(object? sender, object e)
	{
		var view = View;

		// Resolved/committed/unloaded by another path (ViewLoaded re-parent, ReassignParent, HR cascade)
		// in the meantime — nothing left to do.
		if (view is null || !IsLoadedButUnresolved)
		{
			StopResolveWatch();
			return;
		}

		// Cheap, non-mutating probe: has the ancestry up to the navigation root become reachable?
		var reachable = view.FindParentRegion(out _) is not null || view.FindServiceProvider() is not null;
		if (!reachable)
		{
			// Bound the watch so a region genuinely detached from any navigation root (a structural
			// misconfiguration, not a timing race) gives up and logs once rather than watching forever.
			if (++_resolveAttempts >= MaxResolveAttempts)
			{
				StopResolveWatch();

				if (_logger.IsEnabled(LogLevel.Warning))
				{
					_logger.LogWarningMessage($"(Name: {Name}) Region remained unresolved after {MaxResolveAttempts} layout passes; it appears detached from any navigation root and will not host navigation. Visual ancestry: {DescribeAncestry(view)}");
				}
			}

			return;
		}

		// Reachable. Stop watching and complete loading off the layout callback — navigation / visual-tree
		// mutation must not run inside a LayoutUpdated handler (it can fire mid-layout on some platforms;
		// cf. EnsureElementLoaded's Android yield). This is the only dispatcher hop, and only on success.
		StopResolveWatch();
		_resolveAttempts = 0;

		var dispatcher = view.GetDispatcher();
		if (dispatcher is not null)
		{
			dispatcher.TryEnqueue(() => _ = ResolveAndLoadAsync());
		}
		else
		{
			_ = ResolveAndLoadAsync();
		}
	}

	// Re-runs the normal load flow (AssignParent + HandleLoaded) after a late resolution, swallowing any
	// throw — the dispatcher callback is fire-and-forget so an exception would otherwise be unobserved
	// (silent on WASM). AGENTS.md §10: every fire-and-forget MUST have a try/catch.
	private async Task ResolveAndLoadAsync()
	{
		try
		{
			await HandleLoading();
		}
		catch (Exception ex)
		{
			if (_logger.IsEnabled(LogLevel.Error))
			{
				_logger.LogErrorMessage($"(Name: {Name}) Deferred load after late region resolution failed: {ex.GetType().Name}: {ex.Message}");
			}
		}
	}

	// Compact visual-ancestor description for the terminal "unresolved" diagnostic: walks up a bounded
	// number of parents and lists each type, flagging the one (if any) carrying a service provider —
	// distinguishing a missing navigation root (none flagged) from a deeper wiring problem.
	private static string DescribeAncestry(FrameworkElement? start)
	{
		const int maxDepth = 12;

		var sb = new StringBuilder();
		DependencyObject? current = start;
		var depth = 0;
		while (current is not null && depth < maxDepth)
		{
			if (depth > 0)
			{
				sb.Append(" <- ");
			}

			sb.Append(current.GetType().Name);
			if (current.GetServiceProvider() is not null)
			{
				sb.Append("[sp]");
			}

			current = VisualTreeHelper.GetParent(current);
			depth++;
		}

		if (current is not null)
		{
			sb.Append(" <- …");
		}

		return sb.Length > 0 ? sb.ToString() : "(no view)";
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
