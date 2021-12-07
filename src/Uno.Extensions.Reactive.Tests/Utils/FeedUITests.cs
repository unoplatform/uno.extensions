using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests;

public class FeedUITests : FeedTests
{
	/// <inheritdoc />
	[TestInitialize]
	public override void Initialize()
	{
		base.Initialize();

		DispatcherHelper.GetForCurrentThread = () => default;
	}
}
