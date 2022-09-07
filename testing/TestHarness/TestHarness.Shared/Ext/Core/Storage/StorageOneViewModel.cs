using Uno.Extensions.Storage;
using Uno.Extensions.Storage.KeyValueStorage;
using Uno.Extensions.DependencyInjection;

namespace TestHarness.Ext.Navigation.Storage;

[ReactiveBindable(false)]
public partial class StorageOneViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly IServiceProvider _services;
	private readonly IEnumerable<INamedResolver<IKeyValueStorage>> _storage;
	public IEnumerable<INamedResolver<IKeyValueStorage>> Storage => _storage;

	public StorageOneViewModel(INavigator navigator, IServiceProvider services, IEnumerable<INamedResolver<IKeyValueStorage>> storage)
	{
		_navigator = navigator;
		_services = services;
		_storage = storage;
	}

	private INamedResolver<IKeyValueStorage>? _selected;

	[ObservableProperty]
	private string key;

	[ObservableProperty]
	private string value;

	[ObservableProperty]
	private IDictionary<string, string> data;

	[ObservableProperty]
	private KeyValuePair<string, string> selectedPair;

	private IKeyValueStorage? _selectedStorage;

	public INamedResolver<IKeyValueStorage>? Selected
	{
		get => _selected;
		set
		{
			_selected = value;
			_selectedStorage = _selected?.Resolve(_services);
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

}

