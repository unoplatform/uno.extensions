using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Extensions.Navigation;
using Uno.Extensions.Reactive;

namespace Commerce.ViewModels;

public partial class LoginViewModel : IAsyncDisposable
{
	public class BindableLoginViewModel : BindableViewModelBase
	{
		private readonly Bindable<string> _userName;
		private readonly Bindable<string> _password;
		private readonly Bindable<string> _error;

		public BindableLoginViewModel(
			INavigator navigator,
			string? defaultUserName = default,
			string? defaultPassword = default,
			string? defaultError = default)
		{
			_userName = new Bindable<string>(Property<string>(nameof(UserName), defaultUserName, out var userNameSubject));
			_password = new Bindable<string>(Property<string>(nameof(Password), defaultPassword, out var passwordSubject));
			_error = new Bindable<string>(Property<string>(nameof(Error), defaultError, out var errorSubject));

			var login = new CommandBuilder<object?>(nameof(Login));

			var vm = new LoginViewModel(navigator, userNameSubject, passwordSubject, errorSubject, login);
			var ctx = SourceContext.GetOrCreate(vm);
			SourceContext.Set(this, ctx);
			RegisterDisposable(vm);

			Model = vm;
			Login = login.Build(ctx);
		}

		public LoginViewModel Model { get; }

		public string UserName
		{
			get => _userName.GetValue();
			set => _userName.SetValue(value);
		}

		public string Password
		{
			get => _password.GetValue();
			set => _password.SetValue(value);
		}

		public string Error
		{
			get => _error.GetValue();
			set => _error.SetValue(value);
		}

		public IAsyncCommand Login { get; }
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> SourceContext.Find(this)?.DisposeAsync() ?? default;
}
