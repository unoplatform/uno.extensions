using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;

namespace FluentAssertions;

public class MessageRecorderAssertions<T> : GenericCollectionAssertions<IFeedRecorder<T>, Message<T>, MessageRecorderAssertions<T>>
{
	/// <inheritdoc />
	public MessageRecorderAssertions(IFeedRecorder<T> actualValue)
		: base(actualValue)
	{
	}

	public void Be(Action<MessageRecorderConstraintBuilder<T>> constraintsBuilder)
	{
		var builder = new MessageRecorderConstraintBuilder<T>();
		constraintsBuilder(builder);
		var constraint = builder.Build();

		constraint.Assert(Subject);
	}

	public Task BeAsync(Action<MessageRecorderConstraintBuilder<T>> constraintsBuilder)
		=> BeAsync(constraintsBuilder, FeedRecorder.DefaultTimeout, SourceContext.Current.Token);

	public Task BeAsync(Action<MessageRecorderConstraintBuilder<T>> constraintsBuilder, int timeout)
		=> BeAsync(constraintsBuilder, timeout, SourceContext.Current.Token);

	public Task BeAsync(Action<MessageRecorderConstraintBuilder<T>> constraintsBuilder, CancellationToken ct)
		=> BeAsync(constraintsBuilder, FeedRecorder.DefaultTimeout, ct);

	public async Task BeAsync(Action<MessageRecorderConstraintBuilder<T>> constraintsBuilder, int timeout, CancellationToken ct)
	{
		var builder = new MessageRecorderConstraintBuilder<T>();
		constraintsBuilder(builder);
		var constraint = builder.Build();

		await Subject.WaitForMessages(constraint.Messages.Length, timeout, ct);

		constraint.Assert(Subject);
	}
}
