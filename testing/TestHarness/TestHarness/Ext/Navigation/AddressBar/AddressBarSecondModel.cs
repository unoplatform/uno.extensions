namespace TestHarness.Ext.Navigation.AddressBar;

public class AddressBarSecondModel
{
	public AddressBarUser User { get; set; }

	public AddressBarSecondModel(AddressBarUser user)
	{
		User = user;
	}
}

public class AddressBarUser(Guid id, string name)
{
	public Guid UserId { get; set; } = id;
	public string UserName { get; set; } = name;
}
