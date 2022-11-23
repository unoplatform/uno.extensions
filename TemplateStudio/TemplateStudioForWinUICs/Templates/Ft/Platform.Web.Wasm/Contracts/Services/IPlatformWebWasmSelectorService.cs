using Microsoft.UI.Xaml;

namespace Param_RootNamespace.Contracts.Services;

public interface IPlatformWebWasmSelectorService
{
	ElementPlatformWebWasm PlatformWebWasm
	{
        get;
    }

    Task InitializeAsync();

    Task SetPlatformWebWasmAsync(ElementPlatformWebWasm platformWebWasm);

    Task SetRequestedPlatformWebWasmAsync();
}
