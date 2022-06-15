namespace TestHarness.Ext.Authentication.Custom;

public record CustomAuthenticationShellViewModel
{
	private IAuthenticationFlow Flow { get; }
	public CustomAuthenticationShellViewModel(IAuthenticationFlow flow)
	{
		Flow = flow;

		_ = Start();
	}

	private async Task Start()
	{
		await Flow.LaunchAsync();

	}
}
