using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Core.HotReload;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A dependency that will trigger a feed execution when the application is updated (hot-reload).
/// </summary>
internal sealed class HotReloadDependency : IDependency
{
	private static int _count;
	private readonly int _id = Interlocked.Increment(ref _count);

	public HotReloadDependency(FeedSession session)
	{
		var feedDelegate = session.CoreExecutionAction;
		var feedDelegateType = feedDelegate.Method.DeclaringType;

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"[DYNAMIC_RELOAD] #{_id} Dynamic feed listening for updates of delegate {feedDelegateType?.FullName}->{feedDelegate.Method.Name}");
		}

		HotReloadService.ApplicationUpdated += OnUpdate;
		session.Token.Register(() => HotReloadService.ApplicationUpdated -= OnUpdate);

		// Self register as dependency to keep the session active
		session.RegisterDependency(this);

		void OnUpdate(Type[] types)
		{
			if (types.Contains(feedDelegateType))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"[DYNAMIC_RELOAD] #{_id} Delegate {feedDelegateType?.FullName}->{feedDelegate.Method.Name} has been updated");
				}

				session.Execute(new ExecuteRequest(this, "Hot reloading the updated delegate"));
			}
		}
	}

	/// <inheritdoc />
	public async ValueTask OnExecuting(FeedExecution execution, CancellationToken ct)
	{
	}

	/// <inheritdoc />
	public async ValueTask OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct)
	{
	}
}
