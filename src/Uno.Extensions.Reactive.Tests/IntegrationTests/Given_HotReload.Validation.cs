using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Core.HotReload;

namespace Uno.Extensions.Reactive.Tests.IntegrationTests;

partial class Given_HotReload
{
	#region When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed
	private static readonly ConcurrentQueue<string> _originalValidatorCalls = new();

	[TestMethod]
	public async Task When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed()
	{
		_originalValidatorCalls.Clear();
		var sut = await ExecuteOnDispatcher(() => new When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed_MyViewModel());
		var history = new List<string[]>();
		await ExecuteOnDispatcher(() => ((INotifyDataErrorInfo)sut).ErrorsChanged += (snd, e) => history.Add(GetErrorMessages(sut)));

		await WaitForErrors(sut, "original:");

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed_MyModel_v1) });
		await WaitForErrors(sut, "v1:");
		var historyBeforeEdits = await ExecuteOnDispatcher(() => history.Count);

		await ExecuteOnDispatcher(() => sut.Name = "edited");
		await WaitForErrors(sut, "v1:edited");

		await ExecuteOnDispatcher(() => sut.Name = "edited2"); // Marker, the 'edited' message has been processed by all subscribers of the state.
		await WaitForErrors(sut, "v1:edited2");

		_originalValidatorCalls.Should().NotContain("edited", "the validator of the previous version of the model must no longer be used");
		var historyAfterEdits = await ExecuteOnDispatcher(() => history.Skip(historyBeforeEdits).ToArray());
		historyAfterEdits.SelectMany(errors => errors).Should().NotContain(error => error.StartsWith("original:"), "the results of the previous validator must not be published");
	}

	private async Task WaitForErrors(BindableViewModelBase vm, string expected)
	{
		for (var i = 0; i < 500; i++)
		{
			if (await ExecuteOnDispatcher(() => GetErrorMessages(vm)) is [var single] && single == expected)
			{
				return;
			}

			await Task.Delay(10);
		}

		throw new TimeoutException($"Errors are [{string.Join(", ", await ExecuteOnDispatcher(() => GetErrorMessages(vm)))}] while expecting [{expected}].");
	}

	private static string[] GetErrorMessages(BindableViewModelBase vm)
		=> ((INotifyDataErrorInfo)vm).GetErrors("Name").Cast<ValidationResult>().Select(result => result.ErrorMessage ?? "").ToArray();

	[ReactiveBindable(true)]
	public partial class When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed_MyModel
	{
		public IState<string> Name => State.Value(this, () => "")
			.Validate(async (name, ct) =>
			{
				_originalValidatorCalls.Enqueue(name);
				return new[] { new ValidationResult($"original:{name}", new[] { nameof(Name) }) };
			});
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed_MyModel))]
	public partial class When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed_MyModel_v1 : IAsyncDisposable
	{
		internal When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed_MyViewModel __reactiveBindableViewModel = default!;

		public IState<string> Name => State.Value(this, () => "")
			.Validate(async (name, ct) => new[] { new ValidationResult($"v1:{name}", new[] { nameof(Name) }) });

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion
}
