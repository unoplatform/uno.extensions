using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Commands;

partial class AsyncCommand
{
	private sealed class SubCommand
	{
		private readonly CommandConfig _config;
		private readonly AsyncCommand _command;

		private (object? value, bool isValid)? _externalParameter;

		public SubCommand(CommandConfig config, AsyncCommand command)
		{
			_config = config;
			_command = command;
		}

		public async Task SubscribeToParameter(SourceContext context, CancellationToken ct)
		{
			// This method is **hopefully** run on the UI thread  so we use .ConfigureAwait(true)
			// in order to reduce threading issues for _externalParameter and thread changes for UpdateCanExecute().

			if (_config.Parameter is null)
			{
				return;
			}

			_externalParameter = (null, false);
			_command.UpdateCanExecute();

			// Note: We cannot use the FeedUIHelper has we want to filter-out some values (.Where) without coming back on UI thread,
			//		 but this is doing the same a FeedUIHelper.GetSource.
			var parameters = await Task
				.Run(
					() => _config
						.Parameter(context)
						.Where(message => message.Changes.Contains(MessageAxis.Data))
						.ToDeferredEnumerable(),
					ct)
				.ConfigureAwait(true);
			await foreach (var parameter in parameters.WithCancellation(ct).ConfigureAwait(true))
			{
				bool isValid, wasValid = _externalParameter is { isValid: true };
				try
				{
					isValid = parameter.Current.Data.IsSome(out var value);

					if (isValid
						&& _config.CanExecute is not null
						&& _config.ParametersCoercing.TryCoerce(Option.Undefined<object?>(), Option.Some<object?>(value), out var p))
					{
						// If we are not able to coerce parameter here, we consider the value as valid,
						// and we wait for the [Can]Execute to coerce the parameters with parameter from the view.

						isValid = _config.CanExecute(p);
					}

					_externalParameter = (value, isValid);
				}
				catch (Exception error)
				{
					_command.ReportError(error, when: "validating can execute of external parameter");
					_externalParameter = (null, isValid = false);
				}

				if (wasValid != isValid)
				{
					_command.UpdateCanExecute();
				}
			}
		}

		private bool TryCoerceParameter(ref object? parameter)
		{
			if (_externalParameter is { } externalParameter)
			{
				if (!externalParameter.isValid)
				{
					return false;
				}

				if (!_config.ParametersCoercing.TryCoerce(Option.Some(parameter), Option.Some(externalParameter.value), out parameter))
				{
					return false;
				}
			}

			return true;
		}

		public bool CanExecute(object? parameter)
			=> TryCoerceParameter(ref parameter)
				&& !_command.IsExecutingFor(parameter)
				&& (_config.CanExecute?.Invoke(parameter) ?? true);

		public bool TryExecute(object? viewParameter, SourceContext context, CancellationToken ct)
		{
			var coercedParameter = viewParameter;
			if (!TryCoerceParameter(ref coercedParameter)
				|| !(_config.CanExecute?.Invoke(coercedParameter) ?? true))
			{
				return false;
			}

			var executionId = Guid.NewGuid();
			_command.ReportExecutionStarting(executionId, coercedParameter, viewParameter);

			var completed = 0;
			var task = Task.Run(
					async () =>
					{
						using var _ = context.AsCurrent();
						await _config.Execute(coercedParameter, ct);
					},
					ct);

			// Note: As the CT is cancelled when the command is disposed, we have to use a registration instead of a continuation
			// to make sure `ReportExecutionEnded` is run synchronously so before the EventManager is being disposed!
			var ctReg = _command._ct.Token.Register(Complete);

			task.ContinueWith(
				(_, state) =>
				{
					((CancellationTokenRegistration)state!).Dispose();
					Complete();
				},
				ctReg,
				TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);

			void Complete()
			{
				try
				{
					if (Interlocked.CompareExchange(ref completed, 1, 0) is not 0)
					{
						return; // Already completed
					}

					var error = task.Exception switch
					{
						null or { InnerExceptions.Count: 0 } => null,
						{ InnerExceptions.Count: 1 } aggregated => aggregated.InnerExceptions[0],
						{ } aggregated => aggregated,
					};

					_command.ReportExecutionEnded(executionId, coercedParameter, viewParameter, error);
				}
				catch (Exception) { } // Almost impossible, but an error here could crash the app
			}

			return true;
		}
	}
}
