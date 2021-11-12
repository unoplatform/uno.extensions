using System;
using System.Collections.Generic;
using ApplicationTemplate.Views;
using ApplicationTemplate.ViewModels;
using Uno.Extensions.Navigation;

namespace ApplicationTemplate
{
    public class RouterConfiguration : IRouteDefinitions
    {
        public IReadOnlyDictionary<string, (Type, Type)> Routes { get; } = new Dictionary<string, (Type, Type)>()
            .RegisterPage<MainPageViewModel, MainPage>(string.Empty)
            .RegisterPage<SecondPageViewModel, SecondPage>();
    }
}
