

#if WINDOWS
using System.Diagnostics;
using System.Text.Json.Nodes;
using Microsoft.Windows.AppLifecycle;
using Uno.Extensions.Authentication;
using Windows.ApplicationModel.Activation;
using Windows.Security.Authentication.Web;

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
	/// <remarks>Prior to calling this, a call to <see cref="CheckOAuthRedirectionActivation(bool)"/> must be made during application startup.</remarks>
	/// <seealso cref="CheckOAuthRedirectionActivation(bool)"/>
	public static Task<WebAuthenticatorResult> AuthenticateAsync(Uri authorizeUri, Uri callbackUri) => Instance.Authenticate(authorizeUri, callbackUri, CancellationToken.None);

	/// <summary>
	/// Begin an authentication flow by navigating to the specified url and waiting for a callback/redirect to the callbackUrl scheme.
	/// </summary>
	/// <param name="authorizeUri">Url to navigate to, beginning the authentication flow.</param>
	/// <param name="callbackUri">Expected callback url that the navigation flow will eventually redirect to.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Returns a result parsed out from the callback url.</returns>
	/// <remarks>Prior to calling this, a call to <see cref="CheckOAuthRedirectionActivation(bool)"/> must be made during application startup.</remarks>
	/// <seealso cref="CheckOAuthRedirectionActivation(bool)"/>
	public static Task<WebAuthenticatorResult> AuthenticateAsync(Uri authorizeUri, Uri callbackUri, CancellationToken cancellationToken) => Instance.Authenticate(authorizeUri, callbackUri, cancellationToken);

	private static readonly WebAuthenticator Instance = new WebAuthenticator();

	private Dictionary<string, TaskCompletionSource<Uri>> tasks = new Dictionary<string, TaskCompletionSource<Uri>>();

	private WebAuthenticator()
	{
		Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += CurrentAppInstance_Activated;
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
		NameValueCollection? vals = null;
		try
		{
			vals = System.Web.HttpUtility.ParseQueryString(protocolArgs.Uri.Query);
		}
		catch { }
		try
		{
			if (vals is null || !(vals["state"] is string))
			{
				var fragment = protocolArgs.Uri.Fragment;
				if (fragment.StartsWith("#"))
				{
					fragment = fragment.Substring(1);
				}
				vals = System.Web.HttpUtility.ParseQueryString(fragment);
			}
		}
		catch { }
		if (vals != null && vals["state"] is string state)
		{
			try
			{
				var jsonObject = System.Text.Json.Nodes.JsonObject.Parse(state) as JsonObject;
				if (jsonObject is not null)
				{
					NameValueCollection vals2 = new NameValueCollection(jsonObject.Count);
					if (jsonObject.ContainsKey("appInstanceId") && jsonObject["appInstanceId"] is JsonValue jvalue && jvalue.TryGetValue<string>(out string? value))
						vals2.Add("appInstanceId", value);
					if (jsonObject.ContainsKey("signinId") && jsonObject["signinId"] is JsonValue jvalue2 && jvalue2.TryGetValue<string>(out string? value2))
						vals2.Add("signinId", value2);
					return vals2;
				}
			}
			catch { }
		}
		return null;
	}
	private static bool _oauthCheckWasPerformed;

	/// <summary>
	/// Performs an OAuth protocol activation check and redirects activation to the correct application instance.
	/// </summary>
	/// <param name="skipShutDownOnActivation">If <c>true</c>, this application instance will not automatically be shut down. If set to
	/// <c>true</c> ensure you handle instance exit, or you'll end up with multiple instances running.</param>
	/// <returns><c>true</c> if the activation was redirected and this instance should be shut down, otherwise <c>false</c>.</returns>
	/// <remarks>
	/// The call to this method should be done preferably in the Program.Main method, or the application constructor. It must be called
	/// prior to using <see cref="AuthenticateAsync(Uri, Uri, CancellationToken)"/>
	/// </remarks>
	/// <seealso cref="AuthenticateAsync(Uri, Uri, CancellationToken)"/>
	public static bool CheckOAuthRedirectionActivation(bool skipShutDownOnActivation = false)
	{
		var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent()?.GetActivatedEventArgs();
		return CheckOAuthRedirectionActivation(activatedEventArgs, skipShutDownOnActivation);
	}

	/// <summary>
	/// Performs an OAuth protocol activation check and redirects activation to the correct application instance.
	/// </summary>
	/// <param name="activatedEventArgs">The activation arguments</param>
	/// <param name="skipShutDownOnActivation">If <c>true</c>, this application instance will not automatically be shut down. If set to
	/// <c>true</c> ensure you handle instance exit, or you'll end up with multiple instances running.</param>
	/// <returns><c>true</c> if the activation was redirected and this instance should be shut down, otherwise <c>false</c>.</returns>
	/// <remarks>
	/// The call to this method should be done preferably in the Program.Main method, or the application constructor. It must be called
	/// prior to using <see cref="AuthenticateAsync(Uri, Uri, CancellationToken)"/>
	/// </remarks>
	/// <seealso cref="AuthenticateAsync(Uri, Uri, CancellationToken)"/>
	public static bool CheckOAuthRedirectionActivation(AppActivationArguments? activatedEventArgs, bool skipShutDownOnActivation = false)
	{
		_oauthCheckWasPerformed = true;
		if (activatedEventArgs is null)
			return false;
		if (activatedEventArgs.Kind != Microsoft.Windows.AppLifecycle.ExtendedActivationKind.Protocol)
			return false;
		var state = GetState(activatedEventArgs);
		if (state is not null && state["appInstanceId"] is string id && state["signinId"] is string signinId && !string.IsNullOrEmpty(signinId))
		{
			var instance = Microsoft.Windows.AppLifecycle.AppInstance.GetInstances().Where(i => i.Key == id).FirstOrDefault();

			if (instance is not null && !instance.IsCurrent)
			{
				// Redirect to correct instance and close this one
				instance.RedirectActivationToAsync(activatedEventArgs).AsTask().Wait();
				if (!skipShutDownOnActivation)
					System.Diagnostics.Process.GetCurrentProcess().Kill();
				return true;
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
		return false;
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

	private async Task<WebAuthenticatorResult> Authenticate(Uri authorizeUri, Uri callbackUri, CancellationToken cancellationToken)
	{
		if (!_oauthCheckWasPerformed)
		{
			throw new InvalidOperationException("OAuth redirection check on app activation was not detected. Please make sure a call to WebAuthenticator.CheckOAuthRedirectionActivation was made during App creation.");
		}
		if (!PlatformHelper.IsAppPackaged)  // Original code uses:  !Helpers.IsAppPackaged
		{
			throw new InvalidOperationException("The WebAuthenticator requires a packaged app with an AppxManifest");
		}
		if (!IsUriProtocolDeclared(callbackUri.Scheme))
		{
			throw new InvalidOperationException($"The URI Scheme {callbackUri.Scheme} is not declared in AppxManifest.xml");
		}
		var g = Guid.NewGuid();
		var taskId = g.ToString();
		UriBuilder b = new UriBuilder(authorizeUri);

		var query = System.Web.HttpUtility.ParseQueryString(authorizeUri.Query);
		var stateJson = new JsonObject
			{
				{ "appInstanceId", Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Key },
				{ "signinId", taskId }
			};
		if (query["state"] is string oldstate && !string.IsNullOrEmpty(oldstate))
		{
			stateJson["state"] = oldstate;
		}

		query["state"] = stateJson.ToJsonString();
		b.Query = query.ToString();
		authorizeUri = b.Uri;

		var tcs = new TaskCompletionSource<Uri>();
		if (cancellationToken.CanBeCanceled)
		{
			cancellationToken.Register(() =>
			{
				tcs.TrySetCanceled();
				if (tasks.ContainsKey(taskId))
					tasks.Remove(taskId);
			});
			if (cancellationToken.IsCancellationRequested)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		var process = new System.Diagnostics.Process();
		process.StartInfo.FileName = "rundll32.exe";
		process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + authorizeUri.ToString();
		process.StartInfo.UseShellExecute = true;
		process.Start();
		tasks.Add(taskId, tcs);
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
	/// <summary>
	/// Initializes a new instance of the <see cref="WebAuthenticatorResult"/> class.
	/// </summary>
	/// <param name="callbackUrl">Callback url</param>
	public WebAuthenticatorResult(Uri callbackUrl)
	{
		var str = string.Empty;
		if (!string.IsNullOrEmpty(callbackUrl.Fragment))
			str = callbackUrl.Fragment.Substring(1);
		else if (!string.IsNullOrEmpty(callbackUrl.Query))
			str = callbackUrl.Query;
		var query = System.Web.HttpUtility.ParseQueryString(str);
		foreach (string key in query.Keys)
		{
			if (key == "state")
			{
				try
				{
					var jsonObject = System.Text.Json.Nodes.JsonObject.Parse(query[key] ?? "{}") as JsonObject;

					if (jsonObject is not null && jsonObject.ContainsKey("state") && jsonObject["state"] is JsonValue jvalue && jvalue.TryGetValue<string>(out string? value))
					{
						Properties[key] = value;
						continue;
					}
				}
				catch { }
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
			if (Properties.TryGetValue("expires_in", out var value))
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
