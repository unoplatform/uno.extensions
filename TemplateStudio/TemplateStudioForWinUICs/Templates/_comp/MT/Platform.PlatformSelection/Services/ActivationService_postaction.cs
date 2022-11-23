namespace Param_RootNamespace.Services;

public class ActivationService : IActivationService
{
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
//{[{
    private readonly IPlatformSelectorService _platformSelectorService;
//}]}
    public ActivationService(/*{[{*/IPlatformSelectorService platformSelectorService/*}]}*/)
    {
		//^^
		//{[{
		_platformSelectorService = platformSelectorService;
//}]}
    }

    private async Task InitializeAsync()
    {
//{[{
        await _platformSelectorService.InitializeAsync().ConfigureAwait(false);
//}]}
    }

    private async Task StartupAsync()
    {
//^^
//{[{
        await _platformSelectorService.SetRequestedPlatformAsync();
//}]}
        await Task.CompletedTask;
    }
}
