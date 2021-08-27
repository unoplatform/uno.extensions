using System;

namespace Uno.Extensions.Navigation
{
    public record NavigationMap(
        string Path,
        Type View,
        Type ViewModel = null,
        Type Data = null)
    { }
}
