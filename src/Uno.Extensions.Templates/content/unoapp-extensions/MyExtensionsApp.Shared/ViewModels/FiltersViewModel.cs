using Uno.Extensions.Reactive;

namespace MyExtensionsApp.ViewModels;

public partial record FiltersViewModel(IInput<Filters> filter);
