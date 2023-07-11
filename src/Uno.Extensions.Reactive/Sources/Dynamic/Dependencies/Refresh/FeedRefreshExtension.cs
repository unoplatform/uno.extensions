//using System;
//using System.Linq;
//using Uno.Extensions.Reactive.Core;

//namespace Uno.Extensions.Reactive.Sources;

//internal sealed class FeedRefreshExtension
//{
//	private readonly IDisposable _registration;
//	private readonly Task? _externalRefreshTask;

//	private RefreshToken _token;
//	private bool _isContextRequestEnded;
//	private bool _isEnabled;

//	/// <inheritdoc />
//	public event EventHandler<LoadRequest>? LoadRequested;

//	public FeedRefreshExtension(FeedSession<,> target, AsyncFeedExtensions.NamedRefreshableSource src)
//	{
//		var context = target.Context;
//		var ct = target.Token;

//		_token = RefreshToken.Initial(src, context);

//		//_externalRefreshTask = externalRefreshSignal?.GetSource(context, ct).ForEachAsync(BeginRefresh, ct);
//		context.Requests<RefreshRequest>(Refresh, ct);
//		context.Requests<EndRequest>(
//			_ =>
//			{
//				_isContextRequestEnded = true;
//				TryComplete(null);
//			},
//			ct);

//		_externalRefreshTask?.ContinueWith(TryComplete, TaskContinuationOptions.ExecuteSynchronously);

//		_registration = target.RegisterReloadTrigger(this);
//	}

//	public void Enable()
//	{
//		_isEnabled = true;
//	}

//	private void Refresh(RefreshRequest request)
//	{
//		if (!_isEnabled)
//		{
//			return;
//		}

//		var refreshedVersion = RefreshToken.InterlockedIncrement(ref _token);

//		request.Register(refreshedVersion);
//		RaiseRequested(refreshedVersion);
//	}
//	private void BeginRefresh(Unit _)
//	{
//		if (!_isEnabled)
//		{
//			return;
//		}

//		var refreshedVersion = RefreshToken.InterlockedIncrement(ref _token);

//		RaiseRequested(refreshedVersion);
//	}

//	void TryComplete(Task? _)
//	{
//		if (_externalRefreshTask is not { IsCompleted: false } && _isContextRequestEnded)
//		{
//			_registration.Dispose();
//		}
//	}

//	private void RaiseRequested(RefreshToken token)
//	{
//		_isEnabled = false;

//		LoadRequested?.Invoke(this, new LoadRequest<TResult> { Reason = b => b.Refreshed(token) });
//	}
//}
