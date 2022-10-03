using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Generator.Utils;

internal static class PathHelper
{
	public static string SanitizeFileName(string filename)
		=> filename.Replace(Path.GetInvalidFileNameChars(), '_');
}
