using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_MessageBuilder : FeedTests
{
	[TestMethod]
	public void When_TransientAxis_Then_Dismissed()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var original = Message<string>.Initial.With().Set(myAxis, new object()).Build();

		var builder = original.With();
		var updated = builder.Build();

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder.Get(myAxis).value.IsSet.Should().BeFalse("transient axis should have been cleared at beginning to reflect resulting state");
		updated.Current[myAxis].IsSet.Should().BeFalse("transient axis should have been cleared on update");
	}

	[TestMethod]
	public async Task When_TransientAxisNoUpdate_Then_NotDismissed_OfT()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var manager = new MessageManager<object, object>();
		manager.Update(current => current.With().Set(myAxis, new object()), CT);
		var original = manager.Current;

		// Note: We need to change at least one value for the Current message to be updated (and the transient axis has been automatically removed)
		MessageBuilder<object, object>? builder = default;
		manager.Update(current => builder = current.With(), CT);
		var updated = manager.Current;

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder!.Get(myAxis).value.IsSet.Should().BeFalse("transient axis should have been cleared at beginning to reflect resulting state");
		updated.Current[myAxis].IsSet.Should().BeTrue("transient axis should have not have been cleared has no valid update was performed");
	}

	[TestMethod]
	public async Task When_TransientAxis_Then_Dismissed_OfT()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var myAxis2 = new MessageAxis<object>("my test axis 2", _ => new object());
		var manager = new MessageManager<object, object>();
		manager.Update(current => current.With().Set(myAxis, new object()), CT);
		var original = manager.Current;

		// Note: We need to change at least one value for the Current message to be updated (and the transient axis has been automatically removed)
		MessageBuilder<object, object>? builder = default;
		manager.Update(current => builder = current.With().Set(myAxis2, new object()), CT);
		var updated = manager.Current;

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder!.Get(myAxis).value.IsSet.Should().BeFalse("transient axis should have been cleared at beginning to reflect resulting state");
		updated.Current[myAxis].IsSet.Should().BeFalse("transient axis should have been cleared on update");
	}

	[TestMethod]
	public async Task When_TransientAxisByParent_Then_NotDismissed_OfT()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var parentMsg = Message<object>.Initial.With().Set(myAxis, new object()).Build();
		var manager = new MessageManager<object, object>();
		manager.Update(current => current.With(parentMsg), CT);
		var original = manager.Current;

		// Note: Even if we don't set any axis in Update, the Current message should be updated only by the fact that the transient axis has been automatically removed
		MessageBuilder<object, object>? builder = default;
		manager.Update(current => builder = current.With(), CT);
		var updated = manager.Current;

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder!.Get(myAxis).value.IsSet.Should().BeTrue("transient axis should have been kept as it was defined on parent");
		updated.Current[myAxis].IsSet.Should().BeTrue("transient axis should have been kept as it was defined on parent");
	}

	[TestMethod]
	public async Task When_TransientAxisByParentAndLocally_Then_NotDismissed_OfT()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var parentMsg = Message<object>.Initial.With().Set(myAxis, new object()).Build();
		var manager = new MessageManager<object, object>();
		manager.Update(current => current.With(parentMsg).Set(myAxis, new object()), CT);
		var original = manager.Current;

		// Note: Even if we don't set any axis in Update, the Current message should be updated only by the fact that the transient axis has been automatically removed
		MessageBuilder<object, object>? builder = default;
		manager.Update(current => builder = current.With(), CT);
		var updated = manager.Current;

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder!.Get(myAxis).value.IsSet.Should().BeTrue("transient axis should have been kept as it was also defined on parent");
		updated.Current[myAxis].IsSet.Should().BeTrue("transient axis should have been kept as it was also defined on parent");
	}

	[TestMethod]
	public async Task When_TransientAxisNoUpdate_Then_NotDismissed_TransactionOfT()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var manager = new MessageManager<object, object>();
		manager.Update(current => current.With().Set(myAxis, new object()), CT);
		var original = manager.Current;

		// Note: Even if we don't set any axis in Update, the Current message should be updated only by the fact that the transient axis has been automatically removed
		// Note 2: No needs to Commit the transaction, manager.Current should already reflect changes
		MessageManager<object, object>.UpdateTransaction.MessageBuilder builder = default;
		using var transaction = manager.BeginUpdate(CT);
		transaction.Update(current => builder = current.With());
		var updated = manager.Current;

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder.Get(myAxis).value.IsSet.Should().BeFalse("transient axis should have been cleared at beginning to reflect resulting state");
		updated.Current[myAxis].IsSet.Should().BeTrue("transient axis should have not have been cleared has no valid update was performed");
	}

	[TestMethod]
	public async Task When_TransientAxis_Then_Dismissed_TransactionOfT()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var myAxis2 = new MessageAxis<object>("my test axis 2", _ => new object());
		var manager = new MessageManager<object, object>();
		manager.Update(current => current.With().Set(myAxis, new object()), CT);
		var original = manager.Current;

		// Note: Even if we don't set any axis in Update, the Current message should be updated only by the fact that the transient axis has been automatically removed
		// Note 2: No needs to Commit the transaction, manager.Current should already reflect changes
		MessageManager<object, object>.UpdateTransaction.MessageBuilder builder = default;
		using var transaction = manager.BeginUpdate(CT);
		transaction.Update(current => builder = current.With().Set(myAxis2, new object()));
		var updated = manager.Current;

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder.Get(myAxis).value.IsSet.Should().BeFalse("transient axis should have been cleared at beginning to reflect resulting state");
		updated.Current[myAxis].IsSet.Should().BeFalse("transient axis should have been cleared on update");
	}

	[TestMethod]
	public async Task When_TransientAxisByParent_Then_NotDismissed_TransactionOfT()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var parentMsg = Message<object>.Initial.With().Set(myAxis, new object()).Build();
		var manager = new MessageManager<object, object>();
		manager.Update(current => current.With(parentMsg).Set(myAxis, new object()), CT);
		var original = manager.Current;

		// Note: Even if we don't set any axis in Update, the Current message should be updated only by the fact that the transient axis has been automatically removed
		// Note 2: No needs to Commit the transaction, manager.Current should already reflect changes
		MessageManager<object, object>.UpdateTransaction.MessageBuilder builder = default;
		using var transaction = manager.BeginUpdate(CT);
		transaction.Update(current => builder = current.With());
		var updated = manager.Current;

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder.Get(myAxis).value.IsSet.Should().BeTrue("transient axis should have been kept as it was defined on parent");
		updated.Current[myAxis].IsSet.Should().BeTrue("transient axis should have been kept as it was defined on parent");
	}

	[TestMethod]
	public async Task When_TransientAxisByParentAndLocally_Then_NotDismissed_TransactionOfT()
	{
		var myAxis = new MessageAxis<object>("my test axis", _ => new object()) { IsTransient = true };
		var parentMsg = Message<object>.Initial.With().Set(myAxis, new object()).Build();
		var manager = new MessageManager<object, object>();
		manager.Update(current => current.With(parentMsg).Set(myAxis, new object()), CT);
		var original = manager.Current;

		// Note: Even if we don't set any axis in Update, the Current message should be updated only by the fact that the transient axis has been automatically removed
		// Note 2: No needs to Commit the transaction, manager.Current should already reflect changes
		MessageManager<object, object>.UpdateTransaction.MessageBuilder builder = default;
		using var transaction = manager.BeginUpdate(CT);
		transaction.Update(current => builder = current.With());
		var updated = manager.Current;

		original.Current[myAxis].IsSet.Should().BeTrue();
		builder.Get(myAxis).value.IsSet.Should().BeTrue("transient axis should have been kept as it was also defined on parent");
		updated.Current[myAxis].IsSet.Should().BeTrue("transient axis should have been kept as it was also defined on parent");
	}
}
