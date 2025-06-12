namespace TestHarness.Ext.Navigation.AddressBar;

public partial class AddressBarHomeModel
{
	public IDictionary<string, object> UserId => new Dictionary<string, object>
	{
		{ "QueryUser.Id", new Guid("8a5c5b2e-ff96-474b-9e4d-65bde598f6bc") }
	};

	public AddressBarUser User => new(new Guid("8a5c5b2e-ff96-474b-9e4d-65bde598f6bc"), "João Rodrigues");

	public static int InstanceCount
	{
		get => ApplicationData.Current.LocalSettings.Values.TryGetValue(Constants.HomeInstanceCountKey, out var value)
			? (int)value
			: 0;
		private set => ApplicationData.Current.LocalSettings.Values[Constants.HomeInstanceCountKey] = value;
	}

	public int InstanceCountProperty { get; private set; }

	public AddressBarHomeModel()
	{
		InstanceCountProperty = ++InstanceCount;
	}
}
