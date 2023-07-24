using System;
using System.Linq;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A dependency that will listen for <see cref="RefreshRequest"/> on source context and will issue a <see cref="RefreshExecuteRequest"/> on session in response.
/// </summary>
internal sealed class RefreshDependency : IDependency
{
	private readonly FeedSession _session;

	private RefreshToken _token;
	private bool _defaultIsEnabled;
	private bool _isEnabled;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="session"></param>
	/// <param name="isEnabledUntilDisabled"></param>
	public RefreshDependency(FeedSession session, bool isEnabledUntilDisabled = false)
	{
		_session = session;
		_token = RefreshToken.Initial(session.Owner, session.Context);
		_defaultIsEnabled = _isEnabled = isEnabledUntilDisabled;

		session.Context.Requests<RefreshRequest>(OnRefreshRequested, session.Token);
		session.Context.Requests<EndRequest>(_ => session.UnRegisterDependency(this), session.Token);
		session.RegisterDependency(this);
	}

	/// <summary>
	/// Enabled the refresh dependency, only for the current execution.
	/// </summary>
	/// <remarks>You have to invoke this at each execution to support refresh request for each values.</remarks>
	public void Enable()
		=> _isEnabled = true;

	/// <summary>
	/// Enables the refresh dependency until it is disabled.
	/// </summary>
	/// <remarks>This will enable the support of refresh request for each execution, even if not re-enabled by a given execution.</remarks>
	public void EnableUntilDisabled()
		=> _isEnabled = _defaultIsEnabled = true;

	/// <summary>
	/// Disables the refresh dependency for the current execution.
	/// </summary>
	public void Disable()
		=> _isEnabled = false;

	/// <summary>
	/// Disables the refresh dependency until it is re-enabled.
	/// </summary>
	/// <remarks>This will disabled the support of refresh request for each execution, unless explicitly if re-enabled for a given execution.</remarks>
	public void DisableUntilEnabled()
		=> _isEnabled = _defaultIsEnabled = false;

	/// <inheritdoc />
	async ValueTask IDependency.OnExecuting(FeedExecution execution, CancellationToken ct)
	{
		_isEnabled = _defaultIsEnabled; // Users as to explicitly enable refresh for the current execution if he desires it.

		if (execution.Requests.OfType<RefreshExecuteRequest>().OrderByDescending(req => req.Token.SequenceId).FirstOrDefault() is { } refreshRequest)
		{
			execution.Enqueue(b => b.Refreshed(refreshRequest.Token));
		}
	}

	/// <inheritdoc />
	async ValueTask IDependency.OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct)
	{
	}

	private void OnRefreshRequested(RefreshRequest req)
	{
		if (_isEnabled)
		{
			var token = RefreshToken.InterlockedIncrement(ref _token);
			req.Register(token);
			_session.Execute(new RefreshExecuteRequest(this, token));
		}
	}

	public record RefreshExecuteRequest(RefreshDependency issuer, RefreshToken Token) : ExecuteRequest(issuer, $"received refresh request '{Token}'");
}
