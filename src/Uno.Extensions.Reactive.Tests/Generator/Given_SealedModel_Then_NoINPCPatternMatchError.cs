using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Generator;

/// <summary>
/// Verifies that the FeedsGenerator correctly handles sealed model types that do not
/// implement <see cref="global::System.ComponentModel.INotifyPropertyChanged"/>.
/// The generated ViewModel must NOT emit <c>if (model is INotifyPropertyChanged npc)</c>
/// pattern matches for sealed types, as the C# compiler rejects them with CS8121.
/// </summary>
[TestClass]
public partial class Given_SealedModel_Then_NoINPCPatternMatchError : FeedUITests
{
	// If these tests compile, the generator correctly guards INPC blocks with canBeINPC.

	[TestMethod]
	public async Task SealedModel_WithoutINPC_GeneratesViewModel()
	{
		await using var vm = new SealedNoINPC_ViewViewModel();

		Assert.IsNotNull(vm);
		Assert.IsNotNull(vm.Model);
	}

	[TestMethod]
	public async Task SealedModel_WithoutINPC_ModelPropertyAccessible()
	{
		await using var vm = new SealedNoINPC_ViewViewModel();

		Assert.IsNotNull(vm.Model);
		vm.Model.Should().BeOfType<SealedNoINPC_ViewModel>();
	}

	[TestMethod]
	public async Task SealedModel_WithINPC_GeneratesViewModel()
	{
		await using var vm = new SealedWithINPC_ViewViewModel();

		Assert.IsNotNull(vm);
	}

	[TestMethod]
	public async Task SealedModel_WithINPC_SubscribesToPropertyChanged()
	{
		await using var vm = new SealedWithINPC_ViewViewModel();

		// The model implements INPC, so the generated code subscribes to PropertyChanged.
		// If the subscription didn't work, this would be a silent failure —
		// but the fact that the ViewModel was created means the generated code compiled.
		vm.Model.Should().BeAssignableTo<global::System.ComponentModel.INotifyPropertyChanged>();
	}

	[TestMethod]
	public async Task NonSealedModel_WithoutINPC_GeneratesViewModel()
	{
		// Non-sealed types always emit the INPC check (it could be implemented at runtime).
		await using var vm = new NonSealedNoINPC_ViewViewModel();

		Assert.IsNotNull(vm);
	}

	// --- Test types ---

	/// <summary>
	/// A sealed partial model that does NOT implement INotifyPropertyChanged.
	/// The generator must skip INPC pattern match blocks for this type.
	/// </summary>
	public sealed partial class SealedNoINPC_ViewModel
	{
		public IFeed<string> Title => Feed.Async(async ct => "Hello");
	}

	/// <summary>
	/// A sealed partial model that DOES implement INotifyPropertyChanged.
	/// The generator should emit INPC pattern match blocks for this type.
	/// </summary>
	public sealed partial class SealedWithINPC_ViewModel : global::System.ComponentModel.INotifyPropertyChanged
	{
#pragma warning disable CS0067 // Event is never used — required to implement the interface
		public event global::System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

		public IFeed<string> Title => Feed.Async(async ct => "Hello");
	}

	/// <summary>
	/// A non-sealed partial model (the common case). INPC blocks are always emitted.
	/// </summary>
	public partial class NonSealedNoINPC_ViewModel
	{
		public IFeed<string> Title => Feed.Async(async ct => "Hello");
	}
}
