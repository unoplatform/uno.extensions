using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Commerce.ViewModels;

public class FilterViewModel : ObservableObject
{
    private string _query;

    public string Query { get => _query; set => SetProperty(ref _query, value); }
    public FilterViewModel(IDictionary<string, object> data)
    {
        if (data.TryGetValue("", out var filter) && filter is string q)
        {
            Query = q;
        }
    }

}
