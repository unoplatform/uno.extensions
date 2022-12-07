//+:cnd:noEmit
global using System.Threading;
global using System.Threading.Tasks;
#if (use-configuration)
global using Microsoft.Extensions.Options;
#endif
global using MyExtensionsApp.Business.Models;
global using MyExtensionsApp.Presentation;
#if (use-http)
global using MyExtensionsApp.Services;
global using Uno.Extensions.Http;
#endif
#if (!use-frame-navigation)
global using Uno.Extensions.Navigation;
#endif
#if (use-http)
global using Refit;
#endif
//-:cnd:noEmit
