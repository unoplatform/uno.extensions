using System;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationMap(
    string Path,
    Type View = null,
    Type ViewModel = null,
    Type Data = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{ }
