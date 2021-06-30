using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Uno.Extensions.Logging;
using Windows.Storage;

namespace Uno.Extensions.Configuration
{
    public class WritableOptions<T> : IWritableOptions<T>
        where T : class, new()
    {
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;
        private readonly Reloader _reloader;

        private readonly ILogger _logger;

        public WritableOptions(
            ILogger<IWritableOptions<T>> logger,
            Reloader reloader,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            _logger = logger;
            _reloader = reloader;
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

        public Task Update(Action<T> applyChanges)
        {
            return Update(options =>
            {
                applyChanges(options);
                return options;
            });
        }

        public async Task Update(Func<T, T> applyChanges)
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

            await _reloader.ReloadAllFileConfigurationProviders(physicalPath);
        }
    }
}
