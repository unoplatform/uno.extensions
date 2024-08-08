global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.Identity.Client;
global using Microsoft.Identity.Client.Extensions.Msal;
global using Uno.Extensions.Authentication;
global using Uno.Extensions.Authentication.MSAL;
global using Uno.Extensions.Configuration;
global using Uno.Extensions.Hosting;
global using Uno.Extensions.Storage;
#if UNO_EXT_MSAL
global using Uno.UI.MSAL;
#endif
global using Windows.Security.Authentication.Web;
