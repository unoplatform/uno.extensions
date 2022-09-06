

#if WINDOWS
using System.Diagnostics;
using Windows.ApplicationModel.Activation;

namespace WinUIEx;

/*******************************************************
 * 
 * This code has been reproduced, with permission, from the WinUIEx repository (https://github.com/dotMorten/WinUIEx)
 * WebAuthenticator: https://github.com/dotMorten/WinUIEx/blob/main/src/WinUIEx/WebAuthenticator.cs
 * WebAuthenticatorResult: https://github.com/dotMorten/WinUIEx/blob/main/src/WinUIEx/WebAuthenticatorResult.cs
 * 
 *******************************************************/

/// <summary>
/// Handles OAuth redirection to the system browser and re-activation.
/// </summary>
/// <remarks>
/// <para>
/// Your app must be configured for OAuth. In you app package's <c>Package.appxmanifest</c> under Declarations, add a 
/// Protocol declaration and add the scheme you registered for your application's oauth redirect url under "Name".
/// </para>
/// </remarks>
public sealed class WebAuthenticator
{
	/// <summary>
	/// Begin an authentication flow by navigating to the specified url and waiting for a callback/redirect to the callbackUrl scheme.
	/// </summary>
	/// <param name="authorizeUri">Url to navigate to, beginning the authentication flow.</param>
	/// <param name="callbackUri">Expected callback url that the navigation flow will eventually redirect to.</param>
	/// <returns>Returns a result parsed out from the callback url.</returns>
	public static Task<WebAuthenticatorResult> AuthenticateAsync(Uri authorizeUri, Uri callbackUri) => Instance.Authenticate(authorizeUri, callbackUri);

	private static readonly WebAuthenticator Instance = new WebAuthenticator();

	private Dictionary<string, TaskCompletionSource<Uri>> tasks = new Dictionary<string, TaskCompletionSource<Uri>>();

	private WebAuthenticator()
	{
		Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += CurrentAppInstance_Activated;
	}

	private static bool _init;

#pragma warning disable CA2255 // The ModuleInitializer attribute should not be used in libraries
	[System.Runtime.CompilerServices.ModuleInitializer]
#pragma warning restore
	public static void Init()
	{
		if (_init)
		{
			return;
		}
		_init = true;
		try
		{
			OnAppCreation();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.WriteLine("WinUIEx: Failed to initialize the WebAuthenticator: " + ex.Message, "WinUIEx");
		}
	}

	private static bool IsUriProtocolDeclared(string scheme)
	{
		if (global::Windows.ApplicationModel.Package.Current is null)
			return false;
		var docPath = Path.Combine(global::Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "AppxManifest.xml");
		var doc = XDocument.Load(docPath, LoadOptions.None);
		var reader = doc.CreateReader();
		var namespaceManager = new XmlNamespaceManager(reader.NameTable);
		namespaceManager.AddNamespace("x", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
		namespaceManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10");

		// Check if the protocol was declared
		var decl = doc.Root?.XPathSelectElements($"//uap:Extension[@Category='windows.protocol']/uap:Protocol[@Name='{scheme}']", namespaceManager);

		return decl != null && decl.Any();
	}

	private static System.Collections.Specialized.NameValueCollection? GetState(Microsoft.Windows.AppLifecycle.AppActivationArguments activatedEventArgs)
	{
		if (activatedEventArgs.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Protocol &&
			activatedEventArgs.Data is IProtocolActivatedEventArgs protocolArgs)
		{
			return GetState(protocolArgs);
		}
		return null;
	}

	private static NameValueCollection? GetState(IProtocolActivatedEventArgs protocolArgs)
	{
		Debug.WriteLine($"args: {protocolArgs.Uri.Query}");
		var vals = System.Web.HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
		if (vals["state"] is string state)
		{
			var vals2 = System.Web.HttpUtility.ParseQueryString(state);
			// Some services doesn't like & encoded state parameters, and breaks them out separately.
			// In that case copy over the important values
			if (vals.AllKeys.Contains("appInstanceId") && !vals2.AllKeys.Contains("appInstanceId"))
				vals2.Add("appInstanceId", vals["appInstanceId"]);
			if (vals.AllKeys.Contains("signinId") && !vals2.AllKeys.Contains("signinId"))
				vals2.Add("signinId", vals["signinId"]);
			return vals2;
		}
		return null;
	}

	private static void OnAppCreation()
	{
		var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent()?.GetActivatedEventArgs();
		if (activatedEventArgs is null)
			return;
		var state = GetState(activatedEventArgs);
		if (state is not null && state["appInstanceId"] is string id && state["signinId"] is string signinId && !string.IsNullOrEmpty(signinId))
		{
			var instance = Microsoft.Windows.AppLifecycle.AppInstance.GetInstances().Where(i => i.Key == id).FirstOrDefault();

			if (instance is not null && !instance.IsCurrent)
			{
				// Redirect to correct instance and close this one
				instance.RedirectActivationToAsync(activatedEventArgs).AsTask().Wait();
				System.Diagnostics.Process.GetCurrentProcess().Kill();
			}
		}
		else
		{
			var thisInstance = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent();
			if (string.IsNullOrEmpty(thisInstance.Key))
			{
				Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey(Guid.NewGuid().ToString());
			}
		}
	}

	private void CurrentAppInstance_Activated(object? sender, Microsoft.Windows.AppLifecycle.AppActivationArguments e)
	{
		if (e.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Protocol)
		{
			if (e.Data is IProtocolActivatedEventArgs protocolArgs)
			{
				var vals = GetState(protocolArgs);
				if (vals is not null && vals["signinId"] is string signinId)
				{
					ResumeSignin(protocolArgs.Uri, signinId);
				}
			}
		}
	}

	private void ResumeSignin(Uri callbackUri, string signinId)
	{
		if (signinId != null && tasks.ContainsKey(signinId))
		{
			var task = tasks[signinId];
			tasks.Remove(signinId);
			task.TrySetResult(callbackUri);
		}
	}

	private async Task<WebAuthenticatorResult> Authenticate(Uri authorizeUri, Uri callbackUri)
	{
		if (global::Windows.ApplicationModel.Package.Current is null)
		{
			throw new InvalidOperationException("The WebAuthenticator requires a packaged app with an AppxManifest");
		}
		if (!IsUriProtocolDeclared(callbackUri.Scheme))
		{
			throw new InvalidOperationException($"The URI Scheme {callbackUri.Scheme} is not declared in AppxManifest.xml");
		}
		var g = Guid.NewGuid();
		UriBuilder b = new UriBuilder(authorizeUri);

		var query = System.Web.HttpUtility.ParseQueryString(authorizeUri.Query);
		var state = $"appInstanceId={Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Key}&signinId={g}";
		if (query["state"] is string oldstate && !string.IsNullOrEmpty(oldstate))
		{
			// Encode the state parameter
			state += "&state=" + System.Web.HttpUtility.UrlEncode(oldstate);
		}
		query["state"] = state;
		b.Query = query.ToString();
		authorizeUri = b.Uri;

		var tcs = new TaskCompletionSource<Uri>();
		var process = new System.Diagnostics.Process();
		process.StartInfo.FileName = "rundll32.exe";
		process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + authorizeUri.ToString();
		process.StartInfo.UseShellExecute = true;
		process.Start();
		tasks.Add(g.ToString(), tcs);
		var uri = await tcs.Task.ConfigureAwait(false);
		return new WebAuthenticatorResult(uri);
	}
}

/// <summary>
/// Web Authenticator result parsed from the callback Url.
/// </summary>
/// <seealso cref="WebAuthenticator"/>
public class WebAuthenticatorResult
{
	public Uri? RawCallbackUrl { get; }
	/// <summary>
	/// Initializes a new instance of the <see cref="WebAuthenticatorResult"/> class.
	/// </summary>
	/// <param name="callbackUrl">Callback url</param>
	public WebAuthenticatorResult(Uri callbackUrl)
	{
		RawCallbackUrl = callbackUrl;

		var query = new NameValueCollection();

		// Retrieve from fragment
		if (!string.IsNullOrEmpty(callbackUrl.Fragment) && callbackUrl.Fragment.Length>1)
		{
			var frag = callbackUrl.Fragment.Substring(1);
			query = System.Web.HttpUtility.ParseQueryString(frag);
		}

		// Retrieve from query
		if (!string.IsNullOrEmpty(callbackUrl.Query))
		{
			var str = callbackUrl.Query;
			var q = System.Web.HttpUtility.ParseQueryString(str);
			foreach (string key in q.Keys)
			{
				query[key] = q[key];
			}
		}

		foreach (string key in query.Keys)
		{
			if (key == "state")
			{
				var values = System.Web.HttpUtility.ParseQueryString(query[key] ?? string.Empty);
				if (values["state"] is string state)
				{
					Properties[key] = state;
				}
				continue;
			}
			Properties[key] = query[key] ?? String.Empty;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WebAuthenticatorResult"/> class.
	/// </summary>
	/// <param name="values">Values from the authentication callback url</param>
	public WebAuthenticatorResult(Dictionary<string, string> values)
	{
		foreach (var value in values)
			Properties[value.Key] = value.Value;
	}

	/// <summary>
	/// The dictionary of key/value pairs parsed form the callback URI's querystring.
	/// </summary>
	public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

	/// <summary>
	/// Gets the value for the <c>access_token</c> key.
	/// </summary>
	/// <value>Access Token parsed from the callback URI <c>access_token</c> parameter.</value>
	public string AccessToken => GetValue("access_token");

	/// <summary>
	/// Gets the value for the <c>refresh_token</c> key.
	/// </summary>
	/// <value>Refresh Token parsed from the callback URI <c>refresh_token</c> parameter.</value>
	public string RefreshToken => GetValue("refresh_token");

	/// <summary>
	/// Gets the value for the <c>id_token</c> key.
	/// </summary>
	public string IdToken => GetValue("id_token");

	/// <summary>
	/// Gets the expiry date as calculated by the timestamp of when the result was created plus the value in seconds for the <c>expires_in</c> key.
	/// </summary>
	/// <value>Timestamp of the creation of the object instance plus the <c>expires_in</c> seconds parsed from the callback URI.</value>
	public DateTimeOffset? RefreshTokenExpiresIn
	{
		get
		{
			if (Properties.TryGetValue("refresh_token_expires_in", out var value))
			{
				if (int.TryParse(value, out var i))
					return DateTimeOffset.UtcNow.AddSeconds(i);
			}

			return null;
		}
	}

	/// <summary>
	/// The expiry date as calculated by the timestamp of when the result was created plus the value in seconds for the <c>expires_in</c> key.
	/// </summary>
	/// <value>Timestamp of the creation of the object instance plus the <c>expires_in</c> seconds parsed from the callback URI.</value>
	public DateTimeOffset? ExpiresIn
	{
		get
		{
			if (Properties.TryGetValue("expires_in", out var value))
			{
				if (int.TryParse(value, out var i))
					return DateTimeOffset.UtcNow.AddSeconds(i);
			}

			return null;
		}
	}

	private string GetValue(string key)
	{
		if (Properties.TryGetValue(key, out var value))
			return value;
		return string.Empty;
	}
}
#endif
