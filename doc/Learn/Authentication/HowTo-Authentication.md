---
uid: Uno.Extensions.Authentication.HowToAuthentication
---
# How-To: Get Started with Authentication

`Uno.Extensions.Authentication` provides you with a consistent way to add authentication to your application. It is recommended to use one of the built-in `IAuthenticationService` implementations. This tutorial will use the custom authorization to validate user credentials.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Basic Credential Checking

- Add `Authentication` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
    +   Authentication;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

- Append `UseAuthentication` to the `IHostBuilder` instance. The `Login` callback is used to verify the credentials. If the user is authenticated, the callback needs to return a non-empty dictionary of key-value pairs (this would typically contain tokens such as an access token and/or refresh token).

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host
                .UseAuthentication(auth =>
                    auth.AddCustom(custom =>
                        custom.Login(
                            async (sp, dispatcher, tokenCache, credentials, cancellationToken) =>
                            {
                                var isValid = credentials.TryGetValue("Username", out var username) && username == "Bob";
                                return isValid ? credentials : default;
                            })
                ));
            });
        ...
    }
    ```

> [!TIP]
> On Apple platforms (iOS, Mac Catalyst) the Uno storage extension, used by the authentication extension, use the OS Key Chain service to store secrets. This requires your application to have the [proper entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements) to work properly.

- Update `MainPage` to accept input via `TextBox` with a binding expression to connect to the `Username` property on the view model. The `Button` is also bound to the `Authenticate` method.

    ```xml
    <TextBox Text="{Binding Username, Mode=TwoWay}" />
    <Button Content="Login"
            Click="{x:Bind ViewModel.Authenticate}" />
    ```

- Update `MainViewModel` to accept an `IAuthenticationService` instance. Note that the `LoginAsync` method requires an `IDispatcher` instance to be supplied, so this is added as a dependency of the `MainViewModel` too.

    ```csharp
    public string? Username { get; set; }

    private readonly IAuthenticationService _auth;
    private readonly IDispatcher _dispatcher;

    public MainViewModel(
        IDispatcher dispatcher,
        INavigator navigator,
        IAuthenticationService auth)
    {
        _auth = auth;
        _dispatcher = dispatcher;
        _navigator = navigator;
    }

    public async Task Authenticate()
    {
        if (await _auth.LoginAsync(_dispatcher, new Dictionary<string, string> { { "Username", Username ?? string.Empty } }, CancellationToken.None))
        {
            await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
    }
    ```

- Update the `Start` method in `ShellViewModel` to invoke Refresh, which will determine if there are valid credentials. If this returns true, can navigate directly to `SecondViewModel`, otherwise to the `MainViewModel`.

    ```csharp
    public async Task Start()
    {
        if (await _auth.RefreshAsync(CancellationToken.None))
        {
            await Navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
        else
        {
            await Navigator.NavigateViewModelAsync<MainViewModel>(this);
        }
    }
    ```

- Update the "Second" route in `App.xaml.cs` to specify that it depends on the "Main" route. This will make sure that even if the app navigates directly to the SecondPage, the MainPage will be added to the back stack.

    ```csharp
    routes
        .Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>() ,
                Nested: new RouteMap[]
                {
                    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
                    new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>(), DependsOn:"Main"),
                }));
    ```

- Update `SecondPage` XAML to include a Button for logging out of the application. This will invoke the `Logout` method on the `SecondViewModel`.

    At this point the application can be run and the user can enter a username and click the Login button. If the name is "Bob" they will be navigated to the `SecondPage`. If the application is restarted the application will automatically navigate to the `SecondPage`, since the user is still logged in.

- The user is likely to want to logout of the application, the `LogoutAsync` method has to be called on the `IAuthenticationService`.

    ```xml
    <Button Content="Logout"
            Click="{x:Bind ViewModel.Logout}" />
    ```

- Add the `Logout` method to the `SecondViewModel`. In this case the `SecondViewModel` has been changed to a record, with properties for the `Dispatcher` and the `IAuthenticationService`.

    ```csharp
    public record SecondViewModel(IDispatcher Dispatcher, IAuthenticationService Auth)
    {
        public async Task Logout()
        {
            await Auth.LogoutAsync(Dispatcher, CancellationToken.None);
        }
    }
    ```

From this walk through, you can see how the IAuthenticationService can be used to authenticate a user using a very simple check on the username. The Login, Refresh, and Logout methods can all be implemented in order to change the behavior of the application.

### 2. Invoking an Authentication Service

- Add `Http` to the `<UnoFeatures>` property in the Class Library (.csproj) file. This will bring the references we need to access `Refit`.

    ```diff
    <UnoFeatures>
        Material;
        Authentication;
    +   Http;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

- Add POCO objects to working with [dummyjson.com](https://dummyjson.com)

    ```csharp
    [Headers("Content-Type: application/json")]
    public interface IDummyJsonEndpoint
    {
        [Post("/auth/login")]
        Task<AuthResponse> Login(Credentials credentials, CancellationToken ct);
    }

    public class Credentials
    {
        [JsonPropertyName("username")]
        public string? Username { get; init; }
        [JsonPropertyName("password")]
        public string? Password { get; init; }
    }

    public class AuthResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
    ```

- Add configuration for Refit endpoints

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host
                .UseHttp((context, services) =>
                    http.AddRefitClient<IDummyJsonEndpoint>(context)
                );
            });
        ...
    }
    ```

- Update `appsettings.json` to include a section that specifies the base Url for the Refit service. Note that the section name needs to match the interface (dropping the leading I) name. In this case, the interface name is `IDummyJsonEndpoint`, so the configuration section is `DummyJsonEndpoint`

    ```json
    {
      "AppConfig": {
        "Title": "AuthSample"
      },
      "LocalizationConfiguration": {
        "Cultures": [ "en" ]
      },
      "DummyJsonEndpoint": {
        "Url": "https://dummyjson.com",
        "UseNativeHandler": true
      }
    }

    ```

- Add the `UseAuthentication` method to the `InitializeHost` method, this time using the `AddCustom` overload that accepts a type parameter. Instead of an `IServiceProvider` being passed into the `Login` callback, an instance of the `IDummyJsonEndpoint` will be provided.

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host
                .UseAuthentication(auth =>
                    auth.AddCustom<IDummyJsonEndpoint>(custom =>
                        custom.Login(
                            async (authService, dispatcher, tokenCache, credentials, cancellationToken) =>
                            {
                                var name = credentials.FirstOrDefault(x => x.Key == "Username").Value;
                                var password = credentials.FirstOrDefault(x => x.Key == "Password").Value;
                                var creds = new Credentials { Username = name, Password = password };
                                var authResponse = await authService.Login(creds, cancellationToken);
                                if (authResponse?.Token is not null)
                                {
                                    credentials["AccessToken"] = authResponse.Token;
                                    return credentials;
                                }
                                return default;
                            })
                ));
            });
        ...
    }
    ```

    In this case, the Username and Password are extracted out of the credentials dictionary and added to an instance of the Credentials class (which we added earlier, along with the IDummyJsonEndpoint interface), which is passed to the Login method.

- Update the MainPage to include a TextBox for entering the password:

    ```xml
    <TextBox Text="{Binding Username, Mode=TwoWay}" />
    <TextBox Text="{Binding Password, Mode=TwoWay}" />
    <Button Content="Login"
            Click="{x:Bind ViewModel.Authenticate}" />
    ```

- Update the Authenticate method on the MainViewModel to pass both username and password to the LoginAsync method

    ```csharp
    public async Task Authenticate()
    {
        if (await _auth.LoginAsync(_dispatcher,
                                    new Dictionary<string, string>
                                    {
                                        { nameof(Username), Username ?? string.Empty },
                                        { nameof(Password), Password ?? string.Empty }
                                    }, CancellationToken.None))
        {
            await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
    }
    ```

With this done, the application has changed from self-validating the username entered by the user, to using a back-end service to perform the validation.
