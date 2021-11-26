using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.View.Utils;

namespace Uno.Extensions.Reactive.Tests;

public class FeedUITests : FeedTests
{
	/// <inheritdoc />
	[TestInitialize]
	public override void Initialize()
	{
		base.Initialize();

		DispatcherHelper.TryEnqueue = (_, callback) => callback();
	}
}
