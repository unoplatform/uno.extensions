using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.Utils;

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
