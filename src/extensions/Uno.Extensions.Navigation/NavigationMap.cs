using System;

namespace Uno.Extensions.Navigation
{
    public record NavigationMap(
        string Path,
        Type View = null,
        Type ViewModel = null,
        Type Data = null)
    { }
}
