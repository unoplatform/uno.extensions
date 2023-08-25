using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Core.HotReload;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.IntegrationTests;

[TestClass]
public partial class Given_HotReload : FeedUITests
{
	[TestMethod]
	public async Task When_UpdateValueTypeFeedInModel_Then_BindableUpdated()
	{
		var bindable = new BindableWhen_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		HotReloadService.UpdateApplication(new[]{typeof(When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel_v1)});

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed == "Feed value from model v1");
	}

	[ReactiveBindable(true)]
	public partial class When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from original model");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(BindableWhen_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel_v1
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from model v1");
	}

	[TestMethod]
	public async Task When_UpdateRecordFeedInModel_Then_BindableUpdated()
	{
		var bindable = new BindableWhen_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel_v1) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed.Value == "Feed value from model v1");
	}

	public record When_UpdateRecordFeedInModel_Then_BindableUpdated_Record(string Value);

	[ReactiveBindable(true)]
	public partial class When_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel
	{
		public IFeed<When_UpdateRecordFeedInModel_Then_BindableUpdated_Record> MyFeed => Feed.Async(async ct => new When_UpdateRecordFeedInModel_Then_BindableUpdated_Record("Feed value from original model"));
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(BindableWhen_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel_v1
	{
		public IFeed<When_UpdateRecordFeedInModel_Then_BindableUpdated_Record> MyFeed => Feed.Async(async ct => new When_UpdateRecordFeedInModel_Then_BindableUpdated_Record("Feed value from model v1"));
	}

	private async Task WaitFor(Func<bool> predicate)
	{
		for (var i = 0; i < 100; i++)
		{
			if (predicate())
			{
				return;
			}

			await Task.Delay(1);
		}

		throw new TimeoutException();
	}
}
