 

# MVUX

 

MVUX is a variation of the MVU design pattern that will also feel familiar to developers who have previously worked with MVVM.

 

Considering a simple application scenario helps to understand what it does and the benefits gained from using it. So let's do that.

Our application will show the current temperature based on an external source.

That should seem simple enough. All it needs to do is put a number on the screen. What's the problem?

As is often the case, there are more details to consider than may be immediately apparent.

What if the external data isn't immediately available when starting the app?
How to show that data is being initially loaded? Or updated?
What if no data is available?
What if there's an error in obtaining or processing the data?
How to keep the app responsive while requesting or updating the UI?
Does the app need to periodically request new data or listen to the external source provide it?
How do we avoid threading or concurrency issues when handling new data in the background?
How do we make sure the code is testable?

Individually, these questions and scenarios are simple to handle, but hopefully, they highlight that there is more to consider in even a very trivial application. Now imagine an application that you need to build, and with more complex data and UIs, the potential for complexity and the amount of required code can grow enormously.

MVUX is a response to this situation and makes it easier to handle many of the above scenarios.

 

We'll look at the pattern and how to use it to make this app.

 

## So, what is MVUX?

It stands for Model, View, Update, eXtended.

Looking at each individual element, it's easiest to start with the View.

The **View** is the UI. You can write this with XAML, C#, or a combination of the two, much as you would when using another design pattern. You can even use data binding to separate the View and the data provided by a ViewModel. Yes, MVUX uses a ViewModel, but in a different way than the MVVM pattern.

When we have our View, the user will interact with it to provide input, or the app will receive input from an external source. Whatever the source of the input, it will trigger an Update.

An **Update** changes the Model, which is then passed to the View. In the standard MVU pattern, an updated model will create a new View from the Model. In MVUX, the existing View will be updated from a generated ViewModel that wraps the Model class. Updates are made to the View to reflect changes or differences with the new ViewModel. The change detection is similar to using the `INotifyPropertyChanged` interface, except MVUX automatically handles the comparison and change detection for you.

The **Model** that the Update creates (and wraps in a generated ViewModel) is typically a simple `class` or `record` that contains only data and no logic. For this reason, a `record` or `struct` is usually the most appropriate data structure for the Model. Having the Model be immutable (or read-only) is also encouraged.

Working with immutable data structures can be a challenging transition for developers who have only previously only worked with Object-Orientated data. The fundamental difference is the creation of a new "object" with different data instead of modifying data within an existing one. Beyond it being a different way of writing and thinking about code, the most common question about creating multiple copies of potentially similar objects is how this impacts memory usage and performance. Don't worry, the "X" part of the MVUX implementation takes care of this for you, and you should not see any negative impact.

In addition to helping avoid possible memory issues, the **eXtended** part of the pattern provides functionality for working with and displaying data.

 

The big difference between MVUX and patterns like MVVM is the use of "feeds" rather than properties. While a property has a single value, a "feed" is similar to a stream of values. When an updated value is received, it replaces the old value, and the View reacts to the change. In this way, it is similar to reactive programming. However, MVUX additionally combines the "feed's" value with a "status." 

Common "statuses" for a "feed" include 'loading', 'has value', 'refreshing', 'empty', and 'error'. MVUX includes controls that make using these different statuses in the View easy without creating additional properties to manipulate and control what to show.

 

A "feed" can be a single value or a collection of values, such as a `List<T>`. It can also keep track of its current value(s). Keeping track of values is necessary if they may need to be "updated" while providing performance optimizations if you only need to display the value in the View without changing it.

Models are always created asynchronously and automatically handle any necessary transitions to the UI thread. 

 

In many ways, MVUX is closer to functional programming than the OO approach often taken by developers using the MVVM pattern. Writing code this way brings benefits in code reuse and can simplify testing.

 

 

The MVUX pattern brings four key benefits over other design patterns used to build native applications.

It's entirely `async` to ensure a responsive user interface.
It is reactive rather than event-driven and so needs less "boilerplate" code.
It encourages the immutability of Model classes which, in turn, can lead to simpler code and easier testing.
It automatically tracks and reports the "status" (or "state") of values which also simplifies the code you need to write.

 

Now you know the fundamentals of the pattern, let's see how to use it to create the simple weather app mentioned above.

 

## Using MVUX to create an app
