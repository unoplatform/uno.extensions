using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Dispatching;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_BindableCollection_Events
{
	[TestMethod]
	public async Task When_UpdateWithoutCollHandler_Then_RaisePropertyChanged()
	{
		var sut = Create<int>();

		// And subscribe to the PropertyChanged (Count)
		var propChanged = false;
		((INotifyPropertyChanged)sut).PropertyChanged += (snd, e) => propChanged = true;

		// ACT - Cause a collection changed
		Switch(sut, 0, 1, 2, 3);

		// ASSERT - Validate that we get the property changed
		Assert.IsTrue(propChanged);
	}

	[TestMethod]
	public async Task When_UpdateWithCollChangedHandlerOnly_Then_RaisePropertyChanged()
	{
		var sut = Create<int>();

		// Create a subscription to the collection changed, but not the VectorChanged
		((INotifyCollectionChanged)sut).CollectionChanged += (snd, e) => { };

		// And subscribe to the PropertyChanged (Count)
		var propChanged = false;
		((INotifyPropertyChanged)sut).PropertyChanged += (snd, e) => propChanged = true;

		// ACT - Cause a collection changed
		Switch(sut, 0, 1, 2, 3);

		// ASSERT - Validate that we get the property changed
		Assert.IsTrue(propChanged);
	}

	[TestMethod]
	public async Task When_UpdateWithVectorChangedHandlerOnly_Then_RaisePropertyChanged()
	{
		var sut = Create<int>();

		// Create a subscription to the vector changed, but not the CollectionChanged
		((IObservableVector<object>)sut).VectorChanged += (snd, e) => { };

		// And subscribe to the PropertyChanged (Count)
		var propChanged = false;
		((INotifyPropertyChanged)sut).PropertyChanged += (snd, e) => propChanged = true;

		// ACT - Cause a collection changed
		Switch(sut, 0, 1, 2, 3);

		// ASSERT - Validate that we get the property changed
		Assert.IsTrue(propChanged);
	}

	[TestMethod]
	public async Task When_UpdateWithCollAndVectorChangedHandlers_Then_RaisePropertyChanged()
	{
		var sut = Create<int>();

		// Create a subscription to the vector and collection changed
		((INotifyCollectionChanged)sut).CollectionChanged += (snd, e) => { };
		((IObservableVector<object>)sut).VectorChanged += (snd, e) => { };

		// And subscribe to the PropertyChanged (Count)
		var propChanged = false;
		((INotifyPropertyChanged)sut).PropertyChanged += (snd, e) => propChanged = true;

		// ACT - Cause a collection changed
		Switch(sut, 0, 1, 2, 3);

		// ASSERT - Validate that we get the property changed
		Assert.IsTrue(propChanged);
	}

	private static object Create<T>()
	{
		var uiAssembly = typeof(BindableListFeed<>).Assembly;
		var type = uiAssembly.GetType("Uno.Extensions.Reactive.Bindings.Collections.BindableCollection", throwOnError: true, ignoreCase: true) ?? throw new InvalidOperationException("Cannot get BindableCollection type.");
		var create = type.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException("Cannot get Create method.");
		var genericCreate = create.MakeGenericMethod(typeof(T));
		var instance = genericCreate.Invoke(null, new object[5]) ?? throw new InvalidOperationException("Failed to create instance of BindableCollection.");

		return instance;
	}

	private static void Switch<T>(object sut, params T[] items)
	{
		var uiAssembly = typeof(BindableListFeed<>).Assembly;
		var observableCollectionType = uiAssembly.GetType("Uno.Extensions.Reactive.Bindings.Collections.ImmutableObservableCollection`1") ?? throw new InvalidOperationException("Cannot ImmutableObservableCollection type.");
		var observableCollectionOfT = observableCollectionType.MakeGenericType(typeof(T));
		var observableCollectionCtor = observableCollectionOfT.GetConstructor(new[] { typeof(IImmutableList<T>) }) ?? throw new InvalidOperationException("Cannot get constructor.");
		var observableCollection = observableCollectionCtor.Invoke(new object[] { items.ToImmutableList() });

		sut
			.GetType()
			.GetMethod("Switch", BindingFlags.Instance | BindingFlags.NonPublic)
			!.Invoke(sut, new[] { observableCollection, null, null });
	}
}
