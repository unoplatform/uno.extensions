---
uid: Uno.Extensions.Mvux.Commands.HowTo
---
# Implementing Commands in MVUX

**Outcome:** A public method on your MVUX model shows up in the ViewModel as a command and can be bound to a `<Button>`.

**Dependencies**

* `Uno.Extensions.Reactive` (or the umbrella Uno.Extensions package your app already uses)

**When to use**

* You have a model method like `Save()` or `DoWork()` and you just want a button to call it.
* You don’t want to hand-write an `ICommand`.

## Steps

1. **Create the model method**

   ```csharp
   using Uno.Extensions.Reactive;
   using System.Threading;

   public partial record MainModel
   {
       public void Save()
       {
           // save logic
       }
   }
   ```

   * MVUX sees `public void Save()` and **generates** a command for the ViewModel named **`Save`**.
   * Generated type is an **`IAsyncCommand`** even for `void` methods, so the button auto-disables while it runs.

2. **Bind in XAML**

   ```xml
   <Button
       Content="Save"
       Command="{Binding Save}" />
   ```

   * Binding is to the **generated** ViewModel, not to the model method directly.
   * When clicked, MVUX calls `MainModel.Save()`.

### Notes

* Applies to: `void`, `Task`, `ValueTask` methods. ([Uno Platform][1])
* If the method is async, add an optional `CancellationToken` as last argument – MVUX will cancel it if the VM is disposed. ([Uno Platform][1])

---

## 2. `02-run-async-model-method-and-auto-disable-button.md`

### Run an async model method and auto-disable the button

**Outcome:** Button disables while your async method runs.

**Dependencies**

* `Uno.Extensions.Reactive`

### Steps

1. **Async method on the model**

   ```csharp
   public partial record MainModel
   {
       public async ValueTask LoadData(CancellationToken ct)
       {
           await Task.Delay(1000, ct);
           // load or refresh
       }
   }
   ```

2. **Bind to generated command**

   ```xml
   <Button
       Content="Refresh"
       Command="{Binding LoadData}" />
   ```

3. **What MVUX does**

   * Generates an `IAsyncCommand LoadData { get; }` on the bindable ViewModel.
   * While `LoadData` runs, the command reports “busy”, so the button is disabled. ([Uno Platform][1])

### Notes

* Works for `Task` and `ValueTask`.

---

### Pass a value from the view to the model command

**Outcome:** You click a button and the model method receives the button’s `CommandParameter`.

**Dependencies**

* `Uno.Extensions.Reactive`

### Steps

1. **Model accepts a parameter**

   ```csharp
   public partial record MainModel
   {
       public void DoWork(double amount)
       {
           // use amount
       }
   }
   ```

   * MVUX generates `public IAsyncCommand DoWork { get; }`.

2. **View sends the parameter**

   ```xml
   <Slider x:Name="AmountSlider" Minimum="1" Maximum="100" />

   <Button
       Content="Apply"
       Command="{Binding DoWork}"
       CommandParameter="{Binding Value, ElementName=AmountSlider}" />
   ```

3. **Command rules**

   * MVUX first checks it **can cast** the parameter to `double`.
   * If parameter is `null` or can’t be cast → command is **disabled**. ([Uno Platform][1])

### Notes

* You can still add a `CancellationToken` as the **last** argument:

  ```csharp
  public ValueTask DoWork(double amount, CancellationToken ct) { ... }
  ```

* View’s `CommandParameter` is **ignored** if the model method doesn’t declare a matching parameter. ([Uno Platform][1])

---

### Use the latest feed value when a command runs

**Outcome:** Your method parameter is auto-filled with the current value of a feed on the same model.

**Dependencies**

* `Uno.Extensions.Reactive`

### When to use

* You have `public IFeed<int> CounterValue => ...;`
* You call `ResetCounter(int counterValue)` and want MVUX to inject the current counter.

### Steps

1. **Model with feed**

   ```csharp
   using Uno.Extensions.Reactive;

   public partial record CounterModel
   {
       public IFeed<int> CounterValue => /* feed source */;

       public void ResetCounter(int counterValue)
       {
           // counterValue is the current value of CounterValue
       }
   }
   ```

2. **Why it works**

   * MVUX looks for a **method parameter** whose **name** and **type** matches a feed property.
   * Name matching is **not case-sensitive**. ([Uno Platform][1])
   * It injects the **generic type** of the feed (`int`), **not** `IFeed<int>`. ([Uno Platform][1])

3. **Bind in XAML**

   ```xml
   <Button
       Content="Reset"
       Command="{Binding ResetCounter}" />
   ```

### Explicit feed mapping

If the names don’t match:

```csharp
public IFeed<int> CounterValue => ...;

[ImplicitFeedCommandParameter(false)]
public void ResetCounter([FeedParameter(nameof(CounterValue))] int newValue)
{
    // newValue is CounterValue's current value
}
```

* `ImplicitFeedCommandParameter(false)` = “don’t auto-match”.
* `[FeedParameter]` = “but do match this one”. ([Uno Platform][1])

---

### Stop MVUX from generating commands

**Outcome:** Public methods stay as methods on the ViewModel; commands are not generated.

**Dependencies**

* `Uno.Extensions.Reactive`

### Option A – turn off on a method

```csharp
public partial record MyModel
{
    [Command(false)]
    public async ValueTask Save()
    {
        // will be exposed as a method on the bindable VM
    }
}
```

* Use this when you want to **x:Bind** to the method directly:

  ```xml
  <Button Click="{x:Bind Save}" />
  ```

### Option B – turn off on a class

```csharp
[ImplicitCommands(false)]
public partial record MyModel
{
    public void DoWork() { ... } // no command generated
}
```

### Option C – turn off for the whole assembly

```csharp
using Uno.Extensions.Reactive;

[assembly: ImplicitCommands(false)]
```

* After this, **only** methods marked with `[Command]` will get a command. ([Uno Platform][1])

---

### Force MVUX to generate a command

**Outcome:** Even if commands are globally disabled, this method still becomes a command.

**Dependencies**

* `Uno.Extensions.Reactive`

### Steps

1. **Disable globally**

   ```csharp
   [assembly: ImplicitCommands(false)]
   ```

2. **Opt-in per method**

   ```csharp
   public partial record MyModel
   {
       [Command] // default is true
       public async ValueTask DoWork()
       {
           // ...
       }
   }
   ```

3. **Use in XAML**

   ```xml
   <Button Command="{Binding DoWork}" Content="Run" />
   ```

* `[Command]` **wins** over `[ImplicitCommands]`. ([Uno Platform][1])

---

### Run a command when a control event happens

**Outcome:** An event like `DoubleTapped` can trigger a generated command and pass a parameter.

**Dependencies**

* `Uno.Microsoft.Xaml.Behaviors.Interactivity.WinUI`
* `Uno.Microsoft.Xaml.Behaviors.WinUI.Managed`
* `Uno.Extensions.Reactive`

### Steps

1. **Model method**

   ```csharp
   public partial record MyModel
   {
       public void TextBlockDoubleTapped(string text)
       {
           // handle double tap
       }
   }
   ```

   * MVUX generates `IAsyncCommand TextBlockDoubleTapped { get; }`

2. **XAML with behaviors**

   ```xml
   <Page
       xmlns:interactivity="using:Microsoft.Xaml.Interactivity"
       xmlns:interactions="using:Microsoft.Xaml.Interactions.Core">

       <TextBlock x:Name="TitleBlock" Text="Double-tap me">
           <interactivity:Interaction.Behaviors>
               <interactions:EventTriggerBehavior EventName="DoubleTapped">
                   <interactions:InvokeCommandAction
                       Command="{Binding TextBlockDoubleTapped}"
                       CommandParameter="{Binding Text, ElementName=TitleBlock}" />
               </interactions:EventTriggerBehavior>
           </interactivity:Interaction.Behaviors>
       </TextBlock>
   </Page>
   ```

* When the event fires, MVUX runs the command and passes the text. ([Uno Platform][1])

---

### Create a command in code (no generation)

**Outcome:** You hand-build a command in the model for special scenarios.

**Dependencies**

* `Uno.Extensions.Reactive`

### When to use

* You need a command that doesn’t come from a public method.
* You want fine control over what `CanExecute` or parameters are.

### Example

```csharp
using Uno.Extensions.Reactive;
using System.Threading;
using System.Windows.Input;

public partial record MyModel
{
    public ICommand PingServerCommand =>
        Command.Async(async ct => await PingServer(ct));

    private ValueTask PingServer(CancellationToken ct)
    {
        // ping logic
        return ValueTask.CompletedTask;
    }
}
```

* `Command.Async(...)` creates an `IAsyncCommand`.
* Bind from XAML:

  ```xml
  <Button
      Content="Ping"
      Command="{Binding Model.PingServerCommand}" />
  ```

  > Note: for **manually created** commands in the model, you bind to `Model.<Command>` from the ViewModel. ([Uno Platform][1])

---

### Create a command that uses feed value + can-execute + async work

**Outcome:** Command takes the current feed value, checks a condition, then runs.

**Dependencies**

* `Uno.Extensions.Reactive`

### Example

```csharp
public partial record PagerModel
{
    public IFeed<int> CurrentPage => /* ... */;

    public IAsyncCommand GoToPageCommand =>
        Command.Create(builder =>
            builder
                .Given(CurrentPage)                     // materialize latest page
                .When(page => page > 0)                 // only if valid
                .Then(async (page, ct) => await NavigateToPage(page, ct))
        );

    private ValueTask NavigateToPage(int page, CancellationToken ct)
    {
        // navigate
        return ValueTask.CompletedTask;
    }
}
```

**XAML**

```xml
<Button
    Content="Go"
    Command="{Binding Model.GoToPageCommand}" />
```

* Because it’s **explicit**, it’s **not** auto-copied on the generated ViewModel – so we bind through `Model.`. ([Uno Platform][1])

---

### Understand MVUX command generation rules

**Outcome:** You know which methods become commands and which don’t.

**Rules** ([Uno Platform][1])

1. Public method on the model.
2. Method returns `void`, `Task`, or `ValueTask`. (Return values are ignored.)
3. Optional **one** `CancellationToken` at the end.
4. Optional **one** view-supplied parameter (from `CommandParameter`).
5. Other parameters can be **feed-resolved** (by name+type).
6. Generation can be turned off (`[ImplicitCommands(false)]` or `[Command(false)]`).
7. Generation can be forced (`[Command]`).

**What you get**

```csharp
public IAsyncCommand MethodName { get; }
```

---

[1]: https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Mvux/Advanced/Commands.html?utm_source=chatgpt.com "Commands"
