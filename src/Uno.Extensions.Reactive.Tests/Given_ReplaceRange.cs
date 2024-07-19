using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Uno.Extensions.Reactive.Bindings.Collections.Services;
using Uno.Extensions.Reactive.Testing;
//using static Uno.Extensions.Reactive.UI.Bindings.Collections.Services.SelectionService;
//using static Uno.Extensions.UI.Reactive.Collections;

namespace Uno.Extensions.Reactive.Tests;

[TestClass]
public class Given_ReplaceRange
{
	private SelectionService _selectionService;

	[TestInitialize]
	public void Initialize()
	{
		AsyncAction<SelectionInfo> asyncAction = async (selectionInfo, token) =>
		{
			await Task.CompletedTask;
		};

		_selectionService = new SelectionService(asyncAction);
	}

	[TestMethod]
	public void ReplaceRange_ShouldReplaceCurrentSelection()
	{
		// Arrange
		var initialSelection = new SelectionInfo(new SelectionIndexRange(2, 3));
		_selectionService.SetFromSource(initialSelection);

		var newRange = new ItemIndexRange(5, 2);

		// Act
		_selectionService.ReplaceRange(newRange);

		// Assert
		var selectedRanges = _selectionService.GetSelectedRanges();
		selectedRanges.Should().HaveCount(1);
		selectedRanges[0].FirstIndex.Should().Be(newRange.FirstIndex);
		selectedRanges[0].Length.Should().Be(newRange.Length);
	}

	[TestMethod]
	public void ReplaceRange_ShouldTriggerStateChanged()
	{
		// Arrange
		var wasStateChangedTriggered = false;
		_selectionService.StateChanged += (s, e) => wasStateChangedTriggered = true;

		var newRange = new ItemIndexRange(1, 1);

		// Act
		_selectionService.ReplaceRange(newRange);

		// Assert
		wasStateChangedTriggered.Should().BeTrue();
	}

	[TestMethod]
	public async Task ReplaceRange_ShouldPushToSource()
	{
		// Arrange
		var newRange = new ItemIndexRange(0, 1);
		var selectionPushed = false;
		AsyncAction<SelectionInfo> asyncAction = async (selectionInfo, token) =>
		{
			selectionPushed = true;
			await Task.CompletedTask;
		};

		_selectionService = new SelectionService(asyncAction);

		// Act
		_selectionService.ReplaceRange(newRange);
		await Task.Delay(100); // Allow time for async operation

		// Assert
		selectionPushed.Should().BeTrue();
	}

	[TestMethod]
	public void ReplaceRange_ShouldClearPreviousSelection()
	{
		// Arrange
		var initialSelection = new SelectionInfo(new SelectionIndexRange(3, 1));
		_selectionService.SetFromSource(initialSelection);

		var newRange = new ItemIndexRange(5, 1);

		// Act
		_selectionService.ReplaceRange(newRange);

		// Assert
		var selectedRanges = _selectionService.GetSelectedRanges();
		selectedRanges.Should().HaveCount(1);
		selectedRanges[0].FirstIndex.Should().Be(newRange.FirstIndex);
		selectedRanges[0].Length.Should().Be(newRange.Length);
	}
}
