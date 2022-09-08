using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

public class RecorderValidator<T> : Constraint<IFeedRecorder<T>>
{
	public MessageValidator<T>[] Messages { get; }

	public RecorderValidator(MessageValidator<T>[] messages)
	{
		Messages = messages;
	}

	public override void Assert(IFeedRecorder<T> recorder)
	{
		try
		{
			using var scope = new AssertionScope(recorder.Name);
			scope.AddReportable("result", recorder.ToString);

			using (AssertionScope.Current.ForContext("messages count"))
			{
				recorder.Count.Should().Be(Messages.Length, because: "we should have same number of messages than defined as expected");
			}

			for (var i = 0; i < Math.Min(recorder.Count, Messages.Length); i++)
			{
				var message = recorder[i];
				var constraint = Messages[i];

				using (AssertionScope.Current.ForContext($"message #{i + 1}/{Messages.Length}"))
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
