using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests.Presentation.Bindings.Collections;

[TestClass]
[RunsOnUIThread]
public partial class Given_BindableListFeed : FeedUITests
{
	public partial class Given_BindableListFeed_Model
	{
		public IListState<int> Items => ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
	}

	[TestMethod]
	public async Task When_GetSourceWithDispatcherInSourceContext_Then_GetCollectionViewForUI()
	{
		var vm = new BindableGiven_BindableListFeed_Model();
		var sut = vm.Items as BindableListFeed<int>;

		sut.Should().NotBeNull();

		var collectionFromContext = await Task.Run(async () => await GetBindableCollection(CreateUIContext()));
		var collectionFromUIThread = await GetBindableCollection(Context);
		var collectionFromBGThread = await Task.Run(async () => await GetBindableCollection(Context));

		collectionFromContext.Should().BeSameAs(collectionFromUIThread);
		collectionFromContext.Should().NotBeSameAs(collectionFromBGThread);

		async Task<object?> GetBindableCollection(SourceContext context)
			=> (await ((ISignal<IMessage>)sut!).GetSource(context).FirstAsync(CT)).Current.Data.SomeOrDefault();
	}

	[TestMethod]
	public async Task When_CollectionChanged_Then_CountPropertyChangedRaised()
	{
		var vm = new BindableGiven_BindableListFeed_Model();
		var sut = vm.Items as BindableListFeed<int>;

		sut.Should().NotBeNull();

		var countHasChanged = false;
		sut!.PropertyChanged += (snd, e) => countHasChanged |= e.PropertyName is null or { Length: 0 } or "Count";

		await Task.Run(async () => await vm.Model.Items.AddAsync(44, CT), CT);

		await UIHelper.WaitFor(() => countHasChanged, CT);
		countHasChanged.Should().BeTrue();
	}
}
