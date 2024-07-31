using System.Diagnostics;
using Uno.Extensions.Reactive;
using Uno.Extensions.Storage;

namespace Playground.ViewModels;

[ReactiveBindable(false)]
public partial class AdHocViewModel : ObservableObject
{
    private readonly IDispatcher _dispatcher;
    private readonly INavigator _navigator;
    private readonly IToDoTaskListEndpoint _todoTaskListEndpoint;
    private readonly ISerializer<Widget> _widgetSerializer;
    private readonly ISerializer<Person> _personSerializer;
    private readonly IAuthenticationTokenProvider _authToken;
    private readonly IStorage _dataService;
    private readonly ISerializer _serializer;
    private readonly NeedsADispatcherService _needsADispatcher;

    [ObservableProperty]
    private string? backgroundTaskProgress;

    public AdHocViewModel(
        IDispatcher dispatcher,
        INavigator navigator,
        IAuthenticationTokenProvider authenticationToken,
        IToDoTaskListEndpoint todoTaskEndpoint,
        ISerializer<Widget> widgetSerializer,
        ISerializer<Person> personSerializer,
        IStorage dataService,
        ISerializer serializer,
        NeedsADispatcherService needsADispatcher)
    {
        _dispatcher = dispatcher;
        _navigator = navigator;
        _authToken = authenticationToken;
        _widgetSerializer = widgetSerializer;
        _personSerializer = personSerializer;
        _todoTaskListEndpoint = todoTaskEndpoint;
        _dataService = dataService;
        _serializer = serializer;
        _needsADispatcher = needsADispatcher;
    }

    public async Task LongRunning()
    {
        await Task.Run(async () =>
        {
            await _navigator.NavigateRouteAsync(this, "./One");
            await Task.Delay(1000);
            await _navigator.NavigateRouteAsync(this, "./Two");
            await Task.Delay(1000);
            await _navigator.NavigateRouteAsync(this, "./Three");
            await Task.Delay(1000);
            await _navigator.NavigateRouteAsync(this, "./One");

        });
    }

    public void RunSerializer()
    {
        var w = new Widget { Name = "Bob", Weight = 60 };
        var str = _widgetSerializer.ToString(w);
        var newW = _widgetSerializer.FromString(str);
        Debug.Assert(w == newW);

        var p = new Person { Name = "Jane", Age = 25, Height = 160.3, Weight = 60 };
        str = _personSerializer.ToString(p);
        var newP = _personSerializer.FromString<Person>(str);
        Debug.Assert(w == newW);
    }

    public async Task FetchTasks()
    {
        var result = await _navigator.NavigateRouteForResultAsync<string>(this, "Auth", qualifier: Qualifiers.Dialog).AsResult();

        if (_authToken is SimpleAuthenticationToken authToken)
        {
            authToken.AccessToken = result.SomeOrDefault() ?? String.Empty;
            var taskLists = await _todoTaskListEndpoint.GetAllAsync(CancellationToken.None);
        }
    }

    public async Task LoadWidgets()
    {
        var widgetsAsText = await _dataService.ReadPackageFileAsync(@"Playground\data.json");

        var widgets = await _dataService.ReadPackageFileAsync<Widget[]>(_serializer, @"Playground\data.json");
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
