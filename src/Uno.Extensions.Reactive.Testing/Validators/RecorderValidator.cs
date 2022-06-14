using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

public class RecorderValidator<T> : Constraint<IFeedRecorder<T>>
{
	private readonly MessageValidator<T>[] _messages;

	public RecorderValidator(MessageValidator<T>[] messages)
	{
		_messages = messages;
	}

	public override void Assert(IFeedRecorder<T> recorder)
	{
		try
		{
			using var scope = new AssertionScope(recorder.Name);
			scope.AddReportable("result", recorder.ToString);

			using (AssertionScope.Current.ForContext("messages count"))
			{
				recorder.Count.Should().Be(_messages.Length, because: "we should have same number of messages than defined as expected");
			}

			for (var i = 0; i < Math.Min(recorder.Count, _messages.Length); i++)
			{
				var message = recorder[i];
				var constraint = _messages[i];

				using (AssertionScope.Current.ForContext($"message #{i + 1}/{_messages.Length}"))
				{
					var previous = i == 0 ? default : recorder[i - 1];

					constraint.Assert(previous, message);
				}
			}
		}
		catch (Exception)
		{
			Console.WriteLine(recorder.ToString());

			throw;
		}
	}
}
