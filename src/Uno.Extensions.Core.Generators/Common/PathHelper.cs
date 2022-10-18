using System;
using System.IO;
using System.Linq;

namespace Uno.Extensions.Generators;

internal static class PathHelper
{
	public static string SanitizeFileName(string filename)
		=> filename.Replace(Path.GetInvalidFileNameChars(), '_');
}
