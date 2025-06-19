---
uid: Uno.Extensions.Storage.GettingStarted
---

# Getting Started with Storage Extensions

> [NOTE]
> To get a overview of the Storage feature capabilities and common file types, please refer to the [Storage Overview](xref:Uno.Extensions.Storage.Overview) page.

## Prerequisites

[!INCLUDE [single-project](../includes/single-project.md)]

[!INCLUDE [create-application](../includes/create-application.md)]

> [!TIP]
> In case you might already have a Uno single-head project, you can still proceed from here to add the Storage feature to your existing project.

So let's get started with a Step-by-Step guide to add the Storage feature to your project!

## 1. Setting up your project

First of all, to set up your project using the Storage feature, you need to follow these steps:

1. `Storage` is provided as an Uno Feature. To enable `Storage` support in your application, add `Storage` to the `<UnoFeatures>` property in your .csproj file.

    ```diff
    <UnoFeatures>
    +    Hosting;
    +    Storage;
    </UnoFeatures>
    ```

2. Add `.UseStorage()` in your `App.xaml.cs` HostBuilder

    ```diff
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
                .UseStorage()
                )
            );
            MainWindow = builder.Window;

    #if DEBUG
            MainWindow.UseStudio();
    #endif
            MainWindow.SetWindowIcon();
            Host = await builder.Build(); // or use `await builder.NavigateAsync<Shell>();` if you want to use Uno.Extensions.Navigation

    ```

3. Inject `IStorage` into your Model's or ViewModel's constructor to enable Dependency Injection (DI) to resolve it automatically.

   ## [MVUX](#tab/mvux)

    ```diff
    public partial record MainModel
    {
    +    private readonly IStorage _storage;

        public MainModel(
    +    IStorage storage)
        {
    +        this._storage = storage;
        }
    ```

   ## [MVVM](#tab/mvvm)

    ```diff
    public partial class MainViewModel
    {
    +    private readonly IStorage _storage;

        public MainViewModel(
    +    IStorage storage)
        {
    +        this._storage = storage;
        }
    ```

---

## 2. Adding files to your project

Now as your Project is set up to use the Storage feature, you can add files to your project.

1. Add the file(s) you want to read or write to in your app to the Application Directory.

    > [!TIP]
    > For the use of best practice, this would be the `Assets\` nested directory(s) of your solution/project structure, but you are free to use any other path of your app.

2. Open your .csproj file again and add the following lines to the `<ItemGroup>` section:

    ```xml
    <ItemGroup>
        <Resource Include="Assets\<YourFolderNameIfAny>\*" CopyToOutputDirectory="PreserveNewest" />
    </ItemGroup>
    ```

    As you may have noticed, the `CopyToOutputDirectory` property is set to `PreserveNewest`, which means that the files will be copied to the output directory only if they are newer than the existing files. This is useful for keeping your output directory clean and avoiding unnecessary file copies, while still ensuring that the latest versions of your files are available at runtime.

    > [!NOTE]
    > Same Properties setting would apply if those files are in any other directory of your project/solution structure.

## Next Steps

Now that you have set up your project and added files to it, you can start using the Storage feature in your application.

Here are some common tasks you might want to do with the Storage feature:

---

[!INCLUDE [getting-help](./includes/getting-help.md)]
