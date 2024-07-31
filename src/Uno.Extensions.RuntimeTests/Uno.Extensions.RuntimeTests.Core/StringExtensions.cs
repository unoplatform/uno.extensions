using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.RuntimeTests;

internal static class StringExtensions
{
	public static bool Contains(this string text, string value, StringComparison comparisonType)
		=> text.IndexOf(value, comparisonType) >= 0;
}
