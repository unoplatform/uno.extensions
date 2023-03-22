---
uid: Overview.Reactive.HowTos.SimpleState
---

# How to create a simple state

In this tutorial you will learn how to create an MVUX project and basic usage of a state (`IState<T>`) and the `FeedView` control.

 - For our data we're going to create a service that retrieves and updates a single value
 that determines the crowdedness of a wedding hall, via a 'remote' service.
 - In this how-to you'll use an `IState<HallCrowdedness>` to asynchronously request and update
 this data from and to the service.
 - How to display the data on the UI
 - How to use the `FeedView` control to display the data and automatically respond to the current feed status
 - See how the UI changes  

## Create the Model

1. Create an MVUX project by following the steps in
[this tutorial](xref:Overview.Reactive.HowTos.CreateMvuxProject), and name your project `TheFancyWeddingHall`.

1. Add a class named *HallCrowdednessService.cs*, and replace its content with the following:

    ```c#
    using System.Threading;
    using System.Threading.Tasks;

    namespace TheFancyWeddingHall;

    public partial record HallCrowdedness(int NumberOfPeopleInHall);

    public class HallCrowdednessService
    {
        // in ideal a service is stateless
        // the local field is for the purpose of this demo 
        private int _numberOfPeopleInHall = 5;

        public ValueTask<HallCrowdedness> GetHallCrowdednessAsync(CancellationToken ct)
        {
            // fake "loading from server"
            var result = new HallCrowdedness(_numberOfPeopleInHall);

            return ValueTask.FromResult(result);
        }

        public ValueTask SetHallCrowdednessAsync(HallCrowdedness crowdedness, CancellationToken ct)
        {
            // fake "updating server"
            _numberOfPeopleInHall = crowdedness.NumberOfPeopleInHall;

            return ValueTask.CompletedTask;
        }
    }
    ```

    We're using a [record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record)
    for the `HallCrowdedness` entity type on purpose,
    as records are designed to be immutable to ensure purity of objects as well as other features.

1. Create a class named *HallCrowdednessModel.cs* and replace its content with the following:

    ```c#
    public partial record HallCrowdednessModel(HallCrowdednessService HallCrowdednessService)
    {   
        public IState<HallCrowdedness> HallCrowdedness => State.Async(this, HallCrowdednessService.GetHallCrowdednessAsync);

        public async ValueTask Save(CancellationToken ct)
        {
            var updatedCrowdedness = await HallCrowdedness;

            await HallCrowdednessService.SetHallCrowdednessAsync(updatedCrowdedness, ct);
        }
    }
    ```

    Unlike feeds, States require a reference to the owner type, so that the data binding can be handled by MVUX.

> [!NOTE]                                                                      
> Feeds and states (`IState<T>` and `IListState<T>` for collections) are used as a gateway
> to asynchronously request data from a service and wrap the result or error (if any) in metadata
> to be displayed in the View in accordingly.
> Learn more about list-feeds [here](xref:Overview.Reactive.HowTos.ListFeed).

> [!TIP]
> Feeds are stateless while States are stateful
> Feeds and are there for when the data from the service is read-only and we're not planning to enable edits to it.
> However States add an extra layer on top of feeds, that enables collecting changes and updating the UI with them,
> and in addition enables easily changing of the state to be reflected on the UI.
> This is part of the magic that automatically does the "Update" part in MVUX for you.

## Data bind the View

The `HallCrowdedness` property in `HallCrowdednessModel`, is an `IState` of type `HallCrowdedness`.  
This is similar in concept to an `IObservable<HallCrowdedness>`, where an `IFeed<HallCrowdedness>`
represents a sequence of values, with access to the additional metadata.

> [!TIP]
> An `IFeed<T>` as well as `IState<T>` are awaitable,
> meaning that to get the value of the feed you would do the following in the model:  
> 
> ```c#
> HallCrowdedness hallCrowdedness = await this.HallCrowdedness;
> ```  

1. In the `Save` method above, place a breakpoint on the line `await _hallCrow...SetHallCrowd...`, for later use:

    ![](Assets/SimpleState-2.jpg)

    MVUX's analyzers will read the `HallCrowdednessModel` and will generate a special mirrored `BindableHallCrowdednessModel`,
    which provides binding capabilities for the View, so that we can stick to sending update message in an MVU fashion.
    
    The `HallCrowdedness` property value also gets cached, so no need to worry about its being created upon each `get`.
    
    In addition, MVUX reads the `Save` method, and generates in the bindable Model a command named `Save`
    that can be used from the View, which is invoked asynchronously.

1. Replace anything inside the `Page` element with the following code:

    ```xaml
    <StackPanel>
        <TextBlock Text="How many people are currently in the hall?" />
        <TextBox 
            DataContext="{Binding HallCrowdedness}"
            Text="{Binding NumberOfPeopleInHall, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />

        <Button Content="Save" Command="{Binding Save}" />
    </StackPanel>
    ```

    As you can see, we're now assigning the generated `Save` command to the button's command.

1. Press <kbd>F7</kbd> to navigate to open code-view, and in the constructor, after the line that calls `InitializeComponent()`,
add the following line:

    ```c#
    this.DataContext = new BindableHallCrowdednessModel(new HallCrowdednessService());
    ```

    The `BindableHallCrowdednessModel` is a special MVUX-generated model proxy class that represents a mirror of the `HallCrowdednessModel` adding binding capabilities,
    for MVUX to be able to recreate and renew the model when an update message is sent by the view.  

1. Click <kbd>F5</kbd> to run the project

1. The app will load with its default value '5' as the number of people.
    
    ![](Assets/SimpleState-1.jpg)

1. Change the number to 15 and click 'Save'.

    The debugger will stop at the breakpoint you placed earlier. <!--(See step No. x)-->
    
    ![](Assets/SimpleState-3.jpg)
    
    As you can see, the current value of the state has gotten the updated number '*15*'.
    This is now being saved to the service, in the following line execution once you hit <kbd>F5</kbd> again.
