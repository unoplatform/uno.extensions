using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uno.Extensions.Logging;

namespace Uno.Extensions.Configuration
{
    public class WritableOptions<T> : IWritableOptions<T>
        where T : class, new()
    {
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;
        private readonly IConfigurationRoot _config;

        private readonly ILogger _logger;

        public WritableOptions(
            ILogger<IWritableOptions<T>> logger,
            IConfigurationRoot configRoot,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            _logger = logger;
            _config = configRoot;
            _options = options;
            _section = section;
            _file = file;
        }

        public T Value
        {
            get
            {
                _logger.LazyLogDebug(() => "Get current value");
                return _options.CurrentValue;
            }
        }

        public T Get(string name)
        {
            _logger.LazyLogDebug(() => $@"Get options with name '{name}'");
            return _options.Get(name);
        }

        public void Update(Action<T> applyChanges)
        {
            Update(options =>
            {
                applyChanges(options);
                return options;
            });
        }

        public void Update(Func<T, T> applyChanges)
        {
            _logger.LazyLogDebug(() => $@"Updating options, saving to file '{_file}'");

            var physicalPath = _file;

            var jObject =
                File.Exists(physicalPath) ?
                JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath)) :
                new JObject();
            var sectionObject = jObject.TryGetValue(_section, out var section) ?
                JsonConvert.DeserializeObject<T>(section.ToString()) : (Value ?? new T());

            sectionObject = applyChanges?.Invoke(sectionObject);

            jObject[_section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(physicalPath, JsonConvert.SerializeObject(jObject, Formatting.Indented));

            _logger.LazyLogDebug(() => $@"Updated options saved, now loading changed configuration");

            var fileProviders = _config.Providers.OfType<FileConfigurationProvider>();
            foreach (var fp in fileProviders)
            {
                _logger.LazyLogDebug(() => $@"Loading from file '{fp.Source.Path}'");

                fp.Load();
            }
            _logger.LazyLogDebug(() => $"Reloading configuration complete");

        }
    }
}
