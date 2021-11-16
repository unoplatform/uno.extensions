using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Options;
using Uno.Extensions.Navigation;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public partial class LoginViewModel : IAsyncDisposable
{
	public class BindableLoginViewModel : BindableViewModelBase
	{
		private readonly BindableCredentials _credentials;
		private readonly Bindable<string> _error;

		public BindableLoginViewModel(
			INavigator navigator,
			IOptions<AppInfo> appInfo,
			Credentials? defaultCredentials = default,
			string? defaultError = default)
		{
			_credentials = new BindableCredentials(Property<Credentials>(nameof(Credentials), defaultCredentials, out var credentialsSubject));
			_error = new Bindable<string>(Property<string>(nameof(Error), defaultError, out var errorSubject));

			var login = new CommandBuilder<object?>(nameof(Login));

			var vm = new LoginViewModel(navigator, appInfo, credentialsSubject, errorSubject, login);
			var ctx = SourceContext.GetOrCreate(vm);
			SourceContext.Set(this, ctx);
			RegisterDisposable(vm);

			Model = vm;
			Login = login.Build(ctx);
		}

		public LoginViewModel Model { get; }

		public BindableCredentials Credentials => _credentials;

		public string Error
		{
			get => _error.GetValue();
			set => _error.SetValue(value);
		}

		public IAsyncCommand Login { get; }
	}

	public class BindableCredentials : Bindable<Credentials>
	{
		private readonly Bindable<string> _username;
		private readonly Bindable<string> _password;

		public BindableCredentials(BindablePropertyInfo<Credentials> property)
			: base(property)
		{
			_username = new Bindable<string>(Property(nameof(UserName), c => c?.UserName, (c, userName) => (c ?? new()) with { UserName = userName }));
			_password = new Bindable<string>(Property(nameof(Password), c => c?.Password, (c, password) => (c ?? new()) with { Password = password }));
		}

		public string? UserName
		{
			get => _username.GetValue();
			set => _username.SetValue(value);
		}

		public string? Password
		{
			get => _password.GetValue();
			set => _password.SetValue(value);
		}
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
