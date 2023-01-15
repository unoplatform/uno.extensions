using Uno.Extensions.Storage;
using Uno.Extensions.Storage.KeyValueStorage;
using Uno.Extensions.DependencyInjection;
using System.Diagnostics;


namespace TestHarness.Ext.Navigation.Storage;

[ReactiveBindable(false)]
public partial class StorageOneViewModel : ObservableObject
{
	private readonly Random _random = new Random();
	private readonly INavigator _navigator;
	private readonly IServiceProvider _services;
	private readonly IEnumerable<INamedInstance<IKeyValueStorage>> _storage;
	public IEnumerable<INamedInstance<IKeyValueStorage>> Storage => _storage;

	[ObservableProperty]
	private string? _perfTestOutput;

	public StorageOneViewModel(INavigator navigator, IServiceProvider services, IEnumerable<INamedInstance<IKeyValueStorage>> storage)
	{
		_navigator = navigator;
		_services = services;
		_storage = storage;
	}

	private INamedInstance<IKeyValueStorage>? _selected;

	[ObservableProperty]
	private string key;

	[ObservableProperty]
	private string value;

	[ObservableProperty]
	private IDictionary<string, string> data;

	[ObservableProperty]
	private KeyValuePair<string, string> selectedPair;

	private IKeyValueStorage? _selectedStorage;

	public INamedInstance<IKeyValueStorage>? Selected
	{
		get => _selected;
		set
		{
			_selected = value;
			_selectedStorage = _selected?.Get();
			_ = LoadAll();
		}
	}

	private async Task LoadAll()
	{
		if (_selectedStorage is null)
		{
			return;
		}

		Data = await _selectedStorage.GetAllValuesAsync(CancellationToken.None);
	}

	public async Task Add()
	{
		if (_selectedStorage is null)
		{
			return;
		}
		await _selectedStorage.SetAsync(Key, Value, CancellationToken.None);

		await LoadAll();
	}

	public async Task PairSelected()
	{
		if (SelectedPair.Key is null || _selectedStorage is null)
		{
			return;
		}
		await _selectedStorage.ClearAsync(SelectedPair.Key, CancellationToken.None);
		await LoadAll();
	}

	public async Task PerfTest()
	{
		if(_selectedStorage is null)
		{
			PerfTestOutput = "Select storage to test";
			return;
		}


		var sw = new Stopwatch();
		sw.Start();
		var iterationCount = 5;
		for (int iterationIndex = 0; iterationIndex < iterationCount; iterationIndex++)
		{
			PerfTestOutput = $"Iteration {iterationIndex}";
			await Task.Delay(50);

			await _selectedStorage.ClearAllAsync(CancellationToken.None);

			var keyCount = 100;
			var keyValues = new List<(string Key, string Value)>();
			for (int i = 0; i < keyCount; i++)
			{
				var key = GenerateString(_random.Next(0, 100));
				var value = GenerateString(_random.Next(0, 1000));
				keyValues.Add((key, value));
			}

			// Set all kv pairs
			foreach (var kv in keyValues)
			{
				await _selectedStorage.SetAsync(kv.Key, kv.Value, CancellationToken.None);
			}

			PerfTestOutput = $"Write complete";
			await Task.Delay(50);

			// Randomly read kv pairs - 5 x keyCount
			var max = (5 * keyCount);
			for (int i = 0; i < max; i++)
			{
				var key = _random.Next(0, keyCount);
				var val = await _selectedStorage.GetAsync<string>(keyValues[key].Key, CancellationToken.None);
				if (i % 20 == 0)
				{
					PerfTestOutput = $"{i}:{max}";
					await Task.Delay(50);
				}
			}
		}

		sw.Stop();
		var totalTime = sw.ElapsedMilliseconds;
		var iterationTime = (int)(totalTime / iterationCount);
		PerfTestOutput = $"{iterationTime}ms";

	}


	public const string Alphabet =
	"abcdefghijklmnopqrstuvwyxzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

	public string GenerateString(int size)
	{
		char[] chars = new char[size];
		for (int i = 0; i < size; i++)
		{
			chars[i] = Alphabet[_random.Next(Alphabet.Length)];
		}
		return new string(chars);
	}
}

