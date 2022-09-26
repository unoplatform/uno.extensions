using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Config;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public class Given_KeyEquatableRecord_Then_Generate
{
	[TestMethod]
	public void When_HasSameId_Then_KeyEquals()
	{
		var inst1 = new MyKeyEquatableRecord(42);
		var inst2 = new MyKeyEquatableRecord(42);

		inst1.KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_HasSameId_Then_SameKeyHashCode()
	{
		var hash1 = new MyKeyEquatableRecord(42).GetKeyHashCode();
		var hash2 = new MyKeyEquatableRecord(42).GetKeyHashCode();

		hash1.Should().Be(hash2);
	}

	[TestMethod]
	public void When_HasDifferentId_Then_NotKeyEquals()
	{
		var inst1 = new MyKeyEquatableRecord(42);
		var inst2 = new MyKeyEquatableRecord(43);

		inst1.KeyEquals(inst2).Should().BeFalse();
	}

	[TestMethod]
	public void When_HasDifferentId_Then_DifferentKeyHashCode()
	{
		var hash1 = new MyKeyEquatableRecord(42).GetKeyHashCode();
		var hash2 = new MyKeyEquatableRecord(43).GetKeyHashCode();

		hash1.Should().NotBe(hash2);
	}

	[TestMethod]
	public void When_IsKeyEquatable_Then_ImplementsInterface()
	{
		(new MyKeyEquatableRecord(42) as IKeyEquatable<MyKeyEquatableRecord>).Should().NotBeNull();
	}

	[TestMethod]
	public void When_IsNotKeyEquatable_Then_DoesNotImplementInterface()
	{
		(new MyNotKeyEquatableRecord(42) as IKeyEquatable<MyKeyEquatableRecord>).Should().BeNull();
	}

	[TestMethod]
	public void When_IsKeyEquatable_Then_HasKeyEqualityComparer()
	{
		KeyEqualityComparer.Find<MyKeyEquatableRecord>().Should().NotBe(null);
	}

	[TestMethod]
	public void When_IsNotKeyEquatable_Then_HasKeyEqualityComparer()
	{
		KeyEqualityComparer.Find<MyNotKeyEquatableRecord>().Should().Be(null);
	}

	[TestMethod]
	public void When_CustomImplicitKeyEquatable_Then_IsKeyEquatable()
	{
		(new MyCustomImplicitKeyEquatableRecord(42, 42) as IKeyEquatable<MyKeyEquatableRecord>).Should().BeNull();
	}

	[TestMethod]
	public void When_CustomImplicitKeyEquatable_Then_CustomKeyUsed()
	{
		var inst1 = new MyCustomImplicitKeyEquatableRecord(42, 1);
		var inst2 = new MyCustomImplicitKeyEquatableRecord(42, 2);

		inst1.GetKeyHashCode().Should().NotBe(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeFalse();
	}

	[TestMethod]
	public void When_CustomImplicitKeyEquatable_Then_OnlyCustomKeyIsUsed()
	{
		var inst1 = new MyCustomImplicitKeyEquatableRecord(1, 42);
		var inst2 = new MyCustomImplicitKeyEquatableRecord(2, 42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_CustomKeyEquatable_Then_CustomKeyUsed()
	{
		var inst1 = new MyCustomKeyEquatableRecord(42, 1);
		var inst2 = new MyCustomKeyEquatableRecord(42, 2);

		inst1.GetKeyHashCode().Should().NotBe(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeFalse();
	}

	[TestMethod]
	public void When_CustomKeyEquatable_Then_OnlyCustomKeyIsUsed()
	{
		var inst1 = new MyCustomKeyEquatableRecord(1, 42);
		var inst2 = new MyCustomKeyEquatableRecord(2, 42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
	}
	[TestMethod]
	public void When_CustomDataAnnotationsKeyEquatable_Then_CustomKeyUsed()
	{
		var inst1 = new MyCustomDataAnnotationsKeyEquatableRecord(42, 1);
		var inst2 = new MyCustomDataAnnotationsKeyEquatableRecord(42, 2);

		inst1.GetKeyHashCode().Should().NotBe(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeFalse();
	}

	[TestMethod]
	public void When_CustomDataAnnotationsKeyEquatable_Then_OnlyCustomKeyIsUsed()
	{
		var inst1 = new MyCustomDataAnnotationsKeyEquatableRecord(1, 42);
		var inst2 = new MyCustomDataAnnotationsKeyEquatableRecord(2, 42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_CustomKeyWithImplicitEquatable_Then_CustomKeyUsed()
	{
		var inst1 = new MyCustomKeyWithImplicitEquatableRecord(42, 1, 1);
		var inst2 = new MyCustomKeyWithImplicitEquatableRecord(42, 2, 2);

		inst1.GetKeyHashCode().Should().NotBe(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeFalse();
	}

	[TestMethod]
	public void When_CustomKeyWithImplicitEquatable_Then_OnlyCustomKeyIsUsed()
	{
		var inst1 = new MyCustomKeyWithImplicitEquatableRecord(1, 42, 1);
		var inst2 = new MyCustomKeyWithImplicitEquatableRecord(2, 42, 2);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_CustomKeyWithoutImplicitEquatable_Then_Generated()
	{
		var inst1 = new MyCustomKeyWithoutImplicitEquatableRecord(1, 42, 1);
		var inst2 = new MyCustomKeyWithoutImplicitEquatableRecord(2, 42, 2);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_RecordStruct_Then_Generated()
	{
		var inst1 = new MyKeyEquatableRecordStruct(42);
		var inst2 = new MyKeyEquatableRecordStruct(42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_CustomImplementedKeyEquatable_Then_OnlyCustomImplIsUsed()
	{
		var inst1 = new MyCustomImplementationEquatableRecord(1, 2);
		var inst2 = new MyCustomImplementationEquatableRecord(2, 1);

		inst1.GetKeyHashCode().Should().Be(3);
		inst1.KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_CustomImplementedKeyEquatable_Then_HasKeyEqualityComparer()
	{
		KeyEqualityComparer.Find<MyCustomImplementationEquatableRecord>().Should().NotBe(null);
	}

	[TestMethod]
	public void When_CustomExplicitlyImplementedKeyEquatable_Then_OnlyCustomImplIsUsed()
	{
		var inst1 = new MyCustomExplicitImplementationEquatableRecord(1, 2);
		var inst2 = new MyCustomExplicitImplementationEquatableRecord(2, 1);

		((IKeyEquatable<MyCustomExplicitImplementationEquatableRecord>)inst1).GetKeyHashCode().Should().Be(3);
		((IKeyEquatable<MyCustomExplicitImplementationEquatableRecord>)inst1).KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_CustomExplicitlyImplementedKeyEquatable_Then_HasKeyEqualityComparer()
	{
		KeyEqualityComparer.Find<MyCustomExplicitImplementationEquatableRecord>().Should().NotBe(null);
	}

	[TestMethod]
	public void When_CustomExplicitlyImplementedEquatableClass_Then_HasKeyEqualityComparer()
	{
		KeyEqualityComparer.Find<MyCustomExplicitImplementationEquatableClass>().Should().NotBe(null);
	}

	[TestMethod]
	public void When_SubWithSameBaseIdButOfDifferentType_Then_NotKeyEquals()
	{
		var inst1 = new MyBaseKeyEquatableRecord(42);
		var inst2 = new MySubKeyEquatableRecord(42, "1");

		inst1.GetKeyHashCode().Should().NotBe(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeFalse();
	}

	[TestMethod]
	public void When_SubKeyEquatableRecord_Then_Generated()
	{
		var inst1 = new MySubKeyEquatableRecord(42, "1");
		var inst2 = new MySubKeyEquatableRecord(42, "2");

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();

		var comparer = KeyEqualityComparer.Find<MySubKeyEquatableRecord>();

		comparer.Should().NotBeNull(because: "we should have fallback on base type comparer");
		comparer!.GetHashCode(inst1).Should().Be(inst1.GetKeyHashCode());
		comparer.Equals(inst1, inst2).Should().Be(true);
	}

	[TestMethod]
	public void When_SubNotKeyEquatableRecord_Then_NotGenerated()
	{
		var inst1 = new MySubNotKeyEquatableRecord(42, "1");
		var inst2 = new MySubNotKeyEquatableRecord(42, "2");

		typeof(MySubNotKeyEquatableRecord).GetMethod(nameof(IKeyEquatable<object>.GetKeyHashCode))!.DeclaringType.Should().Be(typeof(MyBaseKeyEquatableRecord));
		typeof(MySubNotKeyEquatableRecord).GetMethod(nameof(IKeyEquatable<object>.KeyEquals))!.DeclaringType.Should().Be(typeof(MyBaseKeyEquatableRecord));

		// But the base class is IKeyEquatable so :
		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();

		var comparer = KeyEqualityComparer.Find<MySubNotKeyEquatableRecord>();

		comparer.Should().NotBeNull(because: "we should have fallback on base type comparer");
	}

	[TestMethod]
	public void When_SubCustomImplicitKeyEquatableRecord_Then_GeneratedAndUseBaseKeyAndCustomKey()
	{
		var inst1 = new MySubCustomImplicitKeyEquatableRecord(1, 42);
		var inst2 = new MySubCustomImplicitKeyEquatableRecord(1, 42);
		var inst3 = new MySubCustomImplicitKeyEquatableRecord(1, 43);
		var inst4 = new MySubCustomImplicitKeyEquatableRecord(2, 42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
		inst1.KeyEquals(inst3).Should().BeFalse();
		inst1.KeyEquals(inst4).Should().BeFalse();

		var comparer = KeyEqualityComparer.Find<MySubCustomImplicitKeyEquatableRecord>();

		comparer.Should().NotBe(null);
		comparer!.GetHashCode(inst1).Should().Be(inst1.GetKeyHashCode());
		comparer.Equals(inst1, inst2).Should().Be(true);
	}

	[TestMethod]
	public void When_SubCustomKeyEquatableRecord_Then_GeneratedAndUseBaseKeyAndCustomKey()
	{
		var inst1 = new MySubCustomKeyEquatableRecord(1, 42);
		var inst2 = new MySubCustomKeyEquatableRecord(1, 42);
		var inst3 = new MySubCustomKeyEquatableRecord(1, 43);
		var inst4 = new MySubCustomKeyEquatableRecord(2, 42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();

		var comparer = KeyEqualityComparer.Find<MySubCustomKeyEquatableRecord>();

		comparer.Should().NotBe(null);
		comparer!.GetHashCode(inst1).Should().Be(inst1.GetKeyHashCode());
		comparer.Equals(inst1, inst2).Should().Be(true);

		inst1.KeyEquals(inst3).Should().BeFalse();
		inst1.KeyEquals(inst4).Should().BeFalse();
	}

	[TestMethod]
	public void When_SubNot_KeyEquatableRecord_Then_Generated()
	{
		var inst1 = new MySubNot_KeyEquatableRecord(42, "1");
		var inst2 = new MySubNot_KeyEquatableRecord(43, "1");

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();

		var comparer = KeyEqualityComparer.Find<MySubNot_KeyEquatableRecord>();

		comparer.Should().NotBe(null);
		comparer!.GetHashCode(inst1).Should().Be(inst1.GetKeyHashCode());
		comparer.Equals(inst1, inst2).Should().Be(true);
	}

	[TestMethod]
	public void When_SubNot_NotKeyEquatableRecord_Then_NotGenerated()
	{
		typeof(MySubNot_NotKeyEquatableRecord).GetMethod(nameof(IKeyEquatable<object>.GetKeyHashCode)).Should().BeNull();
		typeof(MySubNot_NotKeyEquatableRecord).GetMethod(nameof(IKeyEquatable<object>.KeyEquals)).Should().BeNull();
		KeyEqualityComparer.Find<MySubNot_NotKeyEquatableRecord>().Should().BeNull();
	}

	[TestMethod]
	public void When_SubNot_CustomImplicitKeyEquatableRecord_Then_GeneratedAndUseCustomKey()
	{
		var inst1 = new MySubNot_CustomImplicitKeyEquatableRecord(1, 42);
		var inst2 = new MySubNot_CustomImplicitKeyEquatableRecord(2, 42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();

		var comparer = KeyEqualityComparer.Find<MySubNot_CustomImplicitKeyEquatableRecord>();

		comparer.Should().NotBe(null);
		comparer!.GetHashCode(inst1).Should().Be(inst1.GetKeyHashCode());
		comparer.Equals(inst1, inst2).Should().Be(true);
	}

	[TestMethod]
	public void When_SubNot_CustomKeyEquatableRecord_Then_GeneratedAndUseCustomKey()
	{
		var inst1 = new MySubNot_CustomKeyEquatableRecord(1, 42);
		var inst2 = new MySubNot_CustomKeyEquatableRecord(2, 42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();

		var comparer = KeyEqualityComparer.Find<MySubNot_CustomKeyEquatableRecord>();

		comparer.Should().NotBe(null);
		comparer!.GetHashCode(inst1).Should().Be(inst1.GetKeyHashCode());
		comparer.Equals(inst1, inst2).Should().Be(true);
	}

	[TestMethod]
	public void When_NestedRecord_Then_Generated()
	{
		var inst1 = new MyKeyEqualityTypesContainer.MyNestedKeyEquatableRecord(42);
		var inst2 = new MyKeyEqualityTypesContainer.MyNestedKeyEquatableRecord(42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
	}

	[TestMethod]
	public void When_NestedRecordStruct_Then_Generated()
	{
		var inst1 = new MyKeyEqualityTypesContainer.MyNestedKeyEquatableRecordStruct(42);
		var inst2 = new MyKeyEqualityTypesContainer.MyNestedKeyEquatableRecordStruct(42);

		inst1.GetKeyHashCode().Should().Be(inst2.GetKeyHashCode());
		inst1.KeyEquals(inst2).Should().BeTrue();
	}
}

public partial record MyKeyEquatableRecord(int Id);

[ImplicitKeyEquality(IsEnabled = false)]
public partial record MyNotKeyEquatableRecord(int Id);

[ImplicitKeyEquality("MyKey")]
public partial record MyCustomImplicitKeyEquatableRecord(int Id, int MyKey);

public partial record MyCustomKeyEquatableRecord(int Id, [property:Key] int MyKey);

public partial record MyCustomDataAnnotationsKeyEquatableRecord(int Id, [property: System.ComponentModel.DataAnnotations.KeyAttribute] int MyKey);

[ImplicitKeyEquality("MyOtherKey")]
public partial record MyCustomKeyWithImplicitEquatableRecord(int Id, [property: Key] int MyKey, int MyOtherKey);

[ImplicitKeyEquality(IsEnabled = false)]
public partial record MyCustomKeyWithoutImplicitEquatableRecord(int Id, [property: Key] int MyKey, int MyOtherKey);

public partial record struct MyKeyEquatableRecordStruct(int Id);

public partial record MyCustomImplementationEquatableRecord(int Id, int MySecondKey) : IKeyEquatable<MyCustomImplementationEquatableRecord>
{
	public int GetKeyHashCode()
		=> Id + MySecondKey;

	public bool KeyEquals(MyCustomImplementationEquatableRecord other)
		=> GetKeyHashCode() == other.GetKeyHashCode();
}

public partial record MyCustomExplicitImplementationEquatableRecord(int Id, int MySecondKey) : IKeyEquatable<MyCustomExplicitImplementationEquatableRecord>
{
	int IKeyEquatable<MyCustomExplicitImplementationEquatableRecord>.GetKeyHashCode()
		=> Id + MySecondKey;

	bool IKeyEquatable<MyCustomExplicitImplementationEquatableRecord>.KeyEquals(MyCustomExplicitImplementationEquatableRecord other)
		=> ((IKeyEquatable<MyCustomExplicitImplementationEquatableRecord>)this).GetKeyHashCode() == ((IKeyEquatable<MyCustomExplicitImplementationEquatableRecord>)other).GetKeyHashCode();
}

public class MyCustomExplicitImplementationEquatableClass : IKeyEquatable<MyCustomExplicitImplementationEquatableClass>
{
	/// <inheritdoc />
	public int GetKeyHashCode()
		=> 0;

	/// <inheritdoc />
	public bool KeyEquals(MyCustomExplicitImplementationEquatableClass other)
		=> true;
}



public partial record MyBaseKeyEquatableRecord(int Id);

public partial record MySubKeyEquatableRecord(int Id, string SomethingElse) : MyBaseKeyEquatableRecord(Id);

[ImplicitKeyEquality(IsEnabled = false)]
public partial record MySubNotKeyEquatableRecord(int Id, string SomethingElse) : MyBaseKeyEquatableRecord(Id);

[ImplicitKeyEquality("MyKey")]
public partial record MySubCustomImplicitKeyEquatableRecord(int Id, int MyKey) : MyBaseKeyEquatableRecord(Id);

public partial record MySubCustomKeyEquatableRecord(int Id, [property: Key] int MyKey) : MyBaseKeyEquatableRecord(Id);





[ImplicitKeyEquality(IsEnabled = false)]
public partial record MyBaseNotKeyEquatableRecord(int Id);

public partial record MySubNot_KeyEquatableRecord(int Id, string Key) : MyBaseNotKeyEquatableRecord(Id);

[ImplicitKeyEquality(IsEnabled = false)]
public partial record MySubNot_NotKeyEquatableRecord(int Id, string SomethingElse) : MyBaseNotKeyEquatableRecord(Id);

[ImplicitKeyEquality("MyKey")]
public partial record MySubNot_CustomImplicitKeyEquatableRecord(int Id, int MyKey) : MyBaseNotKeyEquatableRecord(Id);

public partial record MySubNot_CustomKeyEquatableRecord(int Id, [property: Key] int MyKey) : MyBaseNotKeyEquatableRecord(Id);





public partial class MyKeyEqualityTypesContainer
{
	public partial record MyNestedKeyEquatableRecord(int Id);

	public partial record struct MyNestedKeyEquatableRecordStruct(int Id);
}
