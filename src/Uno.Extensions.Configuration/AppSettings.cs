using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Uno.Extensions.Configuration
{
    public class AppSettings
    {
        public const string AppSettingsFileName = "appsettings";

        private static AppSettings[]? _appSettings;

        private readonly Assembly _assembly;

        public AppSettings(string fileName, Assembly assembly)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public string FileName { get; }

        public Stream? GetContent()
        {
            using (var resourceFileStream = _assembly.GetManifestResourceStream(FileName))
            {
                if (resourceFileStream != null)
                {
                    var memoryStream = new MemoryStream();

                    resourceFileStream.CopyTo(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    return memoryStream;
                }

                return null;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Not available for desktop.")]
        public static AppSettings[] AllAppSettings<TApplicationRoot>()
             where TApplicationRoot : class
        {
            if (_appSettings is null)
            {
                var executingAssembly = typeof(TApplicationRoot).Assembly;

                _appSettings = executingAssembly
                    .GetManifestResourceNames()
                    .Where(fileName => fileName.ToUpperInvariant().Contains(AppSettingsFileName.ToUpperInvariant()))
                    .Select(fileName => new AppSettings(fileName, executingAssembly))
                    .ToArray();
            }

            return _appSettings;
        }
    }
}
