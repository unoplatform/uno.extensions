using System.Collections.Immutable;
using System.Diagnostics;
using Uno.Extensions.Storage;

namespace TestHarness.Ext.AdHoc;

[ReactiveBindable(false)]
public partial class AdHocOneViewModel : ObservableObject
{
	private readonly IDispatcher _dispatcher;
	private readonly INavigator _navigator;
	private readonly ISerializer<AdHocWidget> _widgetSerializer;
	private readonly ISerializer<AdHocPerson> _personSerializer;
	private readonly ISerializer<AdHocImmutable> _listSerializer;
	private readonly ISerializer _genericSerializer;
	private readonly IStorage _dataService;
	private readonly ISerializer _serializer;
	private readonly AdHocNeedsADispatcherService _needsADispatcher;

	[ObservableProperty]
	private string? backgroundTaskProgress;

	[ObservableProperty]
	private string? personSerilizationOutput;
	[ObservableProperty]
	private string? widgetSerilizationOutput;
	[ObservableProperty]
	private string? immutableListSerilizationOutput;

	public AdHocOneViewModel(
		IDispatcher dispatcher,
		INavigator navigator,
		ISerializer<AdHocWidget> widgetSerializer,
		ISerializer<AdHocPerson> personSerializer,
		ISerializer<AdHocImmutable> listSerializer,
		ISerializer genericSerializer,
		IStorage dataService,
		ISerializer serializer,
		AdHocNeedsADispatcherService needsADispatcher)
	{
		_dispatcher = dispatcher;
		_navigator = navigator;
		_widgetSerializer = widgetSerializer;
		_personSerializer = personSerializer;
		_listSerializer = listSerializer;
		_genericSerializer = genericSerializer;
		_dataService = dataService;
		_serializer = serializer;
		_needsADispatcher = needsADispatcher;
	}

	public async Task LongRunning()
	{
		await Task.Run(async () =>
		{
			await _navigator.NavigateRouteAsync(this, "./AdHocNestedOne");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./AdHocNestedTwo");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./AdHocNestedThree");
			await Task.Delay(1000);
			await _navigator.NavigateRouteAsync(this, "./AdHocNestedOne");

		});
	}

	public async Task RunSerializer()
	{
		try
		{
			var w = new AdHocWidget { Name = "Bob", Weight = 60 };
			var str = _widgetSerializer.ToString(w);
			var newW = _widgetSerializer.FromString(str);
			Debug.Assert(w == newW);
			WidgetSerilizationOutput = "Widget Serialization Ok";
		}
		catch (Exception ex)
		{
			WidgetSerilizationOutput = $"Error(Widget): {ex.Message}";
		}

		try
		{
			var p = new AdHocPerson { Name = "Jane", Age = 25, Height = 160.3, Weight = 60 };
			var str = _personSerializer.ToString(p);
			var newP = _personSerializer.FromString<AdHocPerson>(str);
			Debug.Assert(p == newP);
			PersonSerilizationOutput = "Person Serialization Ok";
		}
		catch (Exception ex)
		{
			PersonSerilizationOutput = $"Error(Person): {ex.Message}";
		}

		try
		{
			var lst = new AdHocImmutable();
			lst = lst with { Names = lst.Names.Add("Fred") };
			lst = lst with { Names = lst.Names.Add("Bob") };
			var str = _listSerializer.ToString(lst);
			var newLst = _listSerializer.FromString<AdHocImmutable>(str);
			if (newLst?.Names is null ||
				lst.Names.Count != newLst.Names.Count ||
				lst.Names[0] != newLst.Names[0] ||
				lst.Names[1] != newLst.Names[1])
			{
				ImmutableListSerilizationOutput = "Immutable Incorrect Serialization";
			}
			ImmutableListSerilizationOutput = "Immutable Serialization Ok";

			var str2 = _genericSerializer.ToString(lst);
			var newLst2 = _genericSerializer.FromString<AdHocImmutable>(str);
			if (newLst2?.Names is null ||
				lst.Names.Count != newLst2.Names.Count ||
				lst.Names[0] != newLst2.Names[0] ||
				lst.Names[1] != newLst2.Names[1])
			{
				ImmutableListSerilizationOutput = "Immutable (Generic) Incorrect Serialization";
			}
			ImmutableListSerilizationOutput = "Immutable (Generic) Serialization Ok";

		}
		catch (Exception ex)
		{
			ImmutableListSerilizationOutput = $"Error(Immutable): {ex.Message}";
		}
	}


	public async Task LoadWidgets()
	{
		var widgets = await _dataService.ReadPackageFileAsync<AdHocWidget[]>(_serializer, "Ext\\AdHoc\\data.json");
	}

	public async Task RunBackgroundTask()
	{
		await _dispatcher.ExecuteAsync(() => BackgroundTaskProgress = "1 - Starting");
		await Task.Run(async () =>
		{
			await Task.Delay(1000);
			await _dispatcher.ExecuteAsync(() => BackgroundTaskProgress = "2 - In Progress");
			await Task.Delay(1000);
			await _dispatcher.ExecuteAsync(async () =>
			{
				BackgroundTaskProgress = "3 - Executing on UI thread";
				await Task.Delay(1000);
				BackgroundTaskProgress = "4 - UI thread complete";
			});
			await Task.Delay(1000);

			var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
			try
			{
				await _dispatcher.ExecuteAsync(async (t) =>
				{
					BackgroundTaskProgress = "5 - Executing on UI thread (again - with cancellation after 1s)";
					await Task.Delay(3000, t);
					BackgroundTaskProgress = "Should NOT get here";
				}, token);
				await _dispatcher.ExecuteAsync(() => BackgroundTaskProgress = "Should NOT get here");
			}
			catch (Exception ex)
			{
				await _dispatcher.ExecuteAsync(() => BackgroundTaskProgress = $"6 - UI thread cancelled - raises exception {ex.Message}");
			}

			await Task.Delay(1000);
			await _dispatcher.ExecuteAsync(() => BackgroundTaskProgress = "7 - Finishing execution");
			await Task.Delay(1000);
		});

		await _dispatcher.ExecuteAsync(() => BackgroundTaskProgress = "8 - Running something using service with dispatcher");
		var result = await _needsADispatcher.RunSomethingWithDispatcher();

		await _dispatcher.ExecuteAsync(() => BackgroundTaskProgress = $"9 - Completed {result}");
	}
}


public record AdHocPerson
{
	public AdHocPerson() { }

	public AdHocPerson(string name, int age, double height, double weight)
	{
		Name = name;
		Age = age;
		Height = height;
		Weight = weight;
	}
	public string? Name { get; set; }

	public int Age { get; set; }
	public double Height { get; set; }
	public double Weight { get; set; }
}

[JsonSerializable(typeof(AdHocPerson))]
internal partial class AdHocPersonContext : JsonSerializerContext
{ }

public record AdHocImmutable
{
	public IImmutableList<string> Names { get; set; } = ImmutableList<string>.Empty;
}

[JsonSerializable(typeof(AdHocImmutable))]
internal partial class AdHocImmutableContext : JsonSerializerContext
{ }


public record AdHocWidget
{
	public AdHocWidget() { }

	public AdHocWidget(string? name, double weight)
	{
		Name = name;
		Weight = weight;
	}
	public string? Name { get; set; }

	public double Weight { get; set; }
}

[JsonSerializable(typeof(AdHocWidget))]
internal partial class AdHocWidgetContext : JsonSerializerContext
{ }


public class AdHocNeedsADispatcherService
{
	private readonly IDispatcher _dispatcher;
	public AdHocNeedsADispatcherService(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}


	public async Task<string> RunSomethingWithDispatcher()
	{
		await Task.Delay(1000).ConfigureAwait(false);

		return await _dispatcher.ExecuteAsync(async ct =>
		{
			await Task.Delay(1000);
			return "Hi from UI thread";
		});

	}
}
