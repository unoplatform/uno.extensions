using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Uno.Extensions.Configuration
{
    public class WritableOptions<T> : IWritableOptions<T>
        where T : class, new()
    {
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;
        private readonly string _file;
        private readonly IConfigurationRoot _config;

        public WritableOptions(
            IConfigurationRoot configRoot,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            _config = configRoot;
            _options = options;
            _section = section;
            _file = file;
        }

        public T Value => _options.CurrentValue;

        public T Get(string name) => _options.Get(name);

        public void Update(Action<T> applyChanges)
        {
            var physicalPath = _file;

            var jObject =
                File.Exists(physicalPath) ?
                JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath)) :
                new JObject();
            var sectionObject = jObject.TryGetValue(_section, out JToken section) ?
                JsonConvert.DeserializeObject<T>(section.ToString()) : (Value ?? new T());

            applyChanges(sectionObject);

            jObject[_section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(physicalPath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
            var fileProviders = _config.Providers.OfType<FileConfigurationProvider>();
            foreach (var fp in fileProviders)
            {
                fp.Load();
            }
        }
    }
}
