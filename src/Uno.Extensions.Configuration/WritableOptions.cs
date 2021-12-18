using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Uno.Extensions.Logging;

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
				_if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Get current value");
				return _options.CurrentValue;
			}
		}

		public T Get(string name)
		{
			_if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Get options with name '{name}'");
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
			_if(Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($@"Updating options, saving to file '{_file}'");

			var physicalPath = _file;
			var jObject = File.Exists(physicalPath) ? JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(physicalPath)) : new Dictionary<string, object>();
			jObject = jObject ?? new Dictionary<string, object>();
			var sectionObject = Value ?? new T();

			sectionObject = applyChanges?.Invoke(sectionObject) ?? new T();

			jObject[_section] = sectionObject;

			var json = JsonSerializer.Serialize(jObject);
			var dir = Path.GetDirectoryName(physicalPath);
			if (dir is not null && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			File.WriteAllText(physicalPath, json);

			await _reloader.ReloadAllFileConfigurationProviders(physicalPath);
		}
	}
}
