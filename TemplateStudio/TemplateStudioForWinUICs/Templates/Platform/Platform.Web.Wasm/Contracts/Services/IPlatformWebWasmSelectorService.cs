using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformWebWasmService
{
	ElementPlatformWebWasm platformWebWasm
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformWebWasmAsync(ElementPlatformWebWasm platformWebWasm);

    Task SetRequestedPlatformWebWasmAsync();
}
