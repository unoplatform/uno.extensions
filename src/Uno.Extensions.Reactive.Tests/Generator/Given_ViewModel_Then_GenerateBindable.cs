using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public partial class Given_ViewModel_Then_GenerateBindable
{
	public partial class Nested_ViewModel
	{
	}

	internal partial class NestedInternal_ViewModel
	{
	}

	private partial class NestedPrivate_ViewModel
	{
	}

	[ReactiveBindable(false)]
	public partial class NestedFlaggedNoCodeGen_ViewModel
	{
	}

	[ReactiveBindable(false)]
	internal partial class NestedInternalFlaggedNoCodeGen_ViewModel
	{
	}

	[ReactiveBindable(false)]
	private partial class NestedPrivateFlaggedNoCodeGen_ViewModel
	{
	}

	[ReactiveBindable(true)]
	public partial class NestedFlaggedCodeGen_NotSuffixed
	{
	}

	[ReactiveBindable(true)]
	internal partial class NestedInternalFlaggedCodeGen_NotSuffixed
	{
	}

	[ReactiveBindable(true)]
	private partial class NestedPrivateFlaggedCodeGen_NotSuffixed
	{
	}

	[TestMethod]
	public void SimpleViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(Given_ViewModel_Then_GenerateBindable_ViewModel)));

	[TestMethod]
	public void NestedViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(Nested_ViewModel)));

	[TestMethod]
	public void NestedInternalViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(NestedInternal_ViewModel)));

	[TestMethod]
	public void NestedPrivateViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(NestedPrivate_ViewModel)));

	[TestMethod]
	public void FlaggedNoCodeGenViewModel()
		=> Assert.IsNull(GetBindable(typeof(Given_ViewModel_Then_GenerateBindable_FlaggedNoCodeGen_ViewModel)));

	[TestMethod]
	public void NestedFlaggedNoCodeGenViewModel()
		=> Assert.IsNull(GetBindable(typeof(NestedFlaggedNoCodeGen_ViewModel)));

	[TestMethod]
	public void NestedInternalFlaggedNoCodeGenViewModel()
		=> Assert.IsNull(GetBindable(typeof(NestedInternalFlaggedNoCodeGen_ViewModel)));

	[TestMethod]
	public void NestedPrivateFlaggedNoCodeGenViewModel()
		=> Assert.IsNull(GetBindable(typeof(NestedPrivateFlaggedNoCodeGen_ViewModel)));

	[TestMethod]
	public void FlaggedCodeGen_NotSuffixedViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(Given_ViewModel_Then_GenerateBindable_FlaggedCodeGen_NotSuffixed)));

	[TestMethod]
	public void NestedFlaggedCodeGen_NotSuffixedViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(NestedFlaggedCodeGen_NotSuffixed)));

	[TestMethod]
	public void NestedInternalFlaggedCodeGen_NotSuffixedViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(NestedInternalFlaggedCodeGen_NotSuffixed)));

	[TestMethod]
	public void NestedPrivateFlaggedCodeGenView_NotSuffixedViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(NestedPrivateFlaggedCodeGen_NotSuffixed)));

	private Type? GetBindable(Type vmType)
		=> vmType.GetNestedType($"Bindable{vmType.Name}");
}

public partial class Given_ViewModel_Then_GenerateBindable_ViewModel
{
}

[ReactiveBindable(false)]
public partial class Given_ViewModel_Then_GenerateBindable_FlaggedNoCodeGen_ViewModel
{
}

[ReactiveBindable(true)]
public partial class Given_ViewModel_Then_GenerateBindable_FlaggedCodeGen_NotSuffixed
{
}
