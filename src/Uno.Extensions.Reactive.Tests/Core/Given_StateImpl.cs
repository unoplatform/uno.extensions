using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
	public async Task When_Create_Then_TaskDoNotLeak()
	{
		var sut = new StateImpl<string>(Context, Option<string>.None());

		var sub = sut.GetType().GetField("_subscription", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sut)!;
		var src = sub.GetType().GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sub)!;
		var node = src.GetType().GetField("_current", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(src)!;
		var next = node.GetType().GetField("_next", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node)!;
		var task = (Task)next.GetType().GetProperty("Task")!.GetValue(next)!;

		task.CreationOptions
			.HasFlag(TaskCreationOptions.AttachedToParent)
			.Should()
			.BeFalse("Creating the task attached to parent will prevent the current async context to complete as it will wait for the Next task to complete before completing itself.");
	}

	[TestMethod]
	public async Task When_Empty_Then_CanBeUpdatedByMessage()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		await sut.UpdateMessage(msg => msg.Data("42"), CT);

		result.Should().Be(r => r
			.Message(Data.None, Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Empty_Then_CanBeUpdatedByValue()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		await sut.UpdateData(_ => "42", CT);

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

		await sut.UpdateMessage(msg => msg.Data("42"), CT);

		result.Should().Be(r => r
			.Message("0", Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Value_Then_CanBeUpdatedByValue()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.Some("0")).Record();

		await sut.UpdateData(_ => "42", CT);

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
