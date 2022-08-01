#if APPLEAUTH
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticationServices;
using Foundation;
#if __IOS__ || __MACCATALYST__
using UIKit;
using NativeWindow = UIKit.UIWindow;
#else
using AppKit;
using NativeWindow = AppKit.NSWindow;
#endif
#endif

namespace Uno.Extensions.Authentication.Apple;

internal record AppleAuthenticationProvider
(
	IServiceProvider Services,
	ITokenCache Tokens,
	AppleAuthenticationSettings? Settings = null
) : BaseAuthenticationProvider(DefaultName, Tokens)
{
	public const string DefaultName = "Apple";

#if APPLEAUTH
	AuthManager? authManager;

	public async override ValueTask<IDictionary<string, string>?> LoginAsync(
		IDispatcher? dispatcher,
		IDictionary<string, string>? credentials,
		CancellationToken cancellationToken)
	{

#if NET6_0_OR_GREATER
		if (!(OperatingSystem.IsIOSVersionAtLeast(13)) &&
			!(OperatingSystem.IsMacCatalystVersionAtLeast(13, 1)) &&
			!(OperatingSystem.IsMacOSVersionAtLeast(10, 15)))
		{
			return default;
		}
#elif __IOS__
		var ver = UIDevice.CurrentDevice.SystemVersion;
		if (Version.TryParse(ver, out var number) && number.Major < 13)
		{
			return default;
		}
#else
		using var info = new NSProcessInfo();
		if (info.OperatingSystemVersion.Major < 10 ||
		(info.OperatingSystemVersion.Major==10 && info.OperatingSystemVersion.Minor<15))
		{
			return default;
		}
#endif

		var provider = new ASAuthorizationAppleIdProvider();
		var req = provider.CreateRequest();

		authManager = new AuthManager(GetCurrentUIWindow() ?? throw new NullReferenceException());

		var scopes = new List<ASAuthorizationScope>();

		if (Settings?.FullNameScope ?? false)
		{
			scopes.Add(ASAuthorizationScope.FullName);
		}
		if (Settings?.EmailScope ?? false)
		{
			scopes.Add(ASAuthorizationScope.Email);
		}

		req.RequestedScopes = scopes.ToArray();
		var controller = new ASAuthorizationController(new[] { req });

		controller.Delegate = authManager;
		controller.PresentationContextProvider = authManager;

		controller.PerformRequests();

		var creds = await authManager.GetCredentialsAsync();

		if (creds == null)
		{
			return null;
		}

		var idToken = new NSString(creds.IdentityToken!, NSStringEncoding.UTF8).ToString();
		var authCode = new NSString(creds.AuthorizationCode!, NSStringEncoding.UTF8).ToString();
		var name = creds.FullName is not null ?
			NSPersonNameComponentsFormatter.GetLocalizedString(creds.FullName, NSPersonNameComponentsFormatterStyle.Default, 0) :
			string.Empty;

		var tokens = new Dictionary<string, string>();
		tokens["id_token"] = idToken;
		tokens["authorization_code"] = authCode;
		tokens["state"] = creds.State ?? string.Empty;
		tokens["email"] = creds.Email ?? string.Empty;
		tokens["user_id"] = creds.User;
		tokens["name"] = name;
		tokens["realuserstatus"] = creds.RealUserStatus.ToString();

		return tokens;
	}

#if __IOS__ || __MACCATALYST__
	public UIWindow? GetCurrentUIWindow()
	{
		// This call site is reachable on: 'iOS' 10.0 and later.0 'UIApplication.KeyWindow.get' is unsupported on: 'ios' 13.0 and later.
		var window = UIApplication.SharedApplication.KeyWindow;

		if (window != null && window.WindowLevel == UIWindowLevel.Normal)
			return window;

		if (window == null)
		{
			// This call site is reachable on: 'iOS' 10.0 and later. 'UIApplication.Windows.get' is unsupported on: 'ios' 15.0 and later.
			window = UIApplication.SharedApplication
				.Windows
				.OrderByDescending(w => w.WindowLevel)
				.FirstOrDefault(w => w.RootViewController != null && w.WindowLevel == UIWindowLevel.Normal);
		}
		return window;
	}
#else
	public NSWindow? GetCurrentUIWindow()
	{
		var window = NSApplication.SharedApplication.KeyWindow;

		if (window?.Level == NSWindowLevel.Normal)
		{
			return window;
		}

		if (window == null)
		{
			window = NSApplication.SharedApplication.MainWindow;
		}
		return window;
	}


#endif

	class AuthManager : NSObject, IASAuthorizationControllerDelegate, IASAuthorizationControllerPresentationContextProviding
	{
		public Task<ASAuthorizationAppleIdCredential> GetCredentialsAsync()
			=> tcsCredential?.Task!;

		TaskCompletionSource<ASAuthorizationAppleIdCredential> tcsCredential;

		NativeWindow presentingAnchor;

		public AuthManager(NativeWindow presentingWindow)
		{
			tcsCredential = new TaskCompletionSource<ASAuthorizationAppleIdCredential>();
			presentingAnchor = presentingWindow;
		}

		public NativeWindow GetPresentationAnchor(ASAuthorizationController controller)
			=> presentingAnchor;

		[Export("authorizationController:didCompleteWithAuthorization:")]
		public void DidComplete(ASAuthorizationController controller, ASAuthorization authorization)
		{
			var creds = authorization.GetCredential<ASAuthorizationAppleIdCredential>();
			tcsCredential?.TrySetResult(creds!);
		}

		[Export("authorizationController:didCompleteWithError:")]
		public void DidComplete(ASAuthorizationController controller, NSError error)
			=> tcsCredential?.TrySetException(new Exception(error.LocalizedDescription));
	}

#endif

}
