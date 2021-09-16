﻿using System;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationRoute(Uri Uri, object Data = null)
{
}
