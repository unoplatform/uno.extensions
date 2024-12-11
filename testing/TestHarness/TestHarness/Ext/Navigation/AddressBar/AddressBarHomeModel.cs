namespace TestHarness.Ext.Navigation.AddressBar;

public partial class AddressBarHomeModel
{
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
