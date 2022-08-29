using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_StateImpl : FeedTests
{
	[TestMethod]
	public async Task When_Empty_Then_CanBeUpdatedByMessage()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		await sut.UpdateMessage(msg => msg.With().Data("42"), CT);

		result.Should().Be(r => r
			.Message(Data.None, Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Empty_Then_CanBeUpdatedByValue()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		await sut.UpdateValue(_ => "42", CT);

		result.Should().Be(r => r
			.Message(Data.None, Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Empty_Then_CanBeUpdated()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		await sut.Update(_ => "42", CT);

		result.Should().Be(r => r
			.Message(Data.None, Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Value_Then_CanBeUpdatedByMessage()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.Some("0")).Record();

		await sut.UpdateMessage(msg => msg.With().Data("42"), CT);

		result.Should().Be(r => r
			.Message("0", Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Value_Then_CanBeUpdatedByValue()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.Some("0")).Record();

		await sut.UpdateValue(_ => "42", CT);

		result.Should().Be(r => r
			.Message("0", Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Value_Then_CanBeUpdated()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.Some("0")).Record();

		await sut.Update(_ => "42", CT);

		result.Should().Be(r => r
			.Message("0", Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}
}
