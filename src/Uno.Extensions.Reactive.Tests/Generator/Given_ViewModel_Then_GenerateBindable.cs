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
	public void NonPartialViewModel()
		=> Assert.IsNull(GetBindable(typeof(Given_ViewModel_Then_GenerateBindable_NonPartial_ViewModel)));

	[TestMethod]
	public void RecordViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(Given_ViewModel_Then_GenerateBindable_Record_ViewModel)));

	[TestMethod]
	public void RecordNonPartialViewModel()
		=> Assert.IsNull(GetBindable(typeof(Given_ViewModel_Then_GenerateBindable_RecordNonPartial_ViewModel)));

	[TestMethod]
	public void NonPartialNestedViewModel()
		=> Assert.IsNull(GetBindable(typeof(NestedNonPartial_ViewModel)));

	[TestMethod]
	public void NonPartialContainer_PublicViewModel()
		=> Assert.IsNull(GetBindable(typeof(Given_ViewModel_Then_GenerateBindable_NonPartialContainer.Public_ViewModel)));

	[TestMethod]
	public void NonPartialContainer_InternalViewModel()
		=> Assert.IsNull(GetBindable(typeof(Given_ViewModel_Then_GenerateBindable_NonPartialContainer.Internal_ViewModel)));

	[TestMethod]
	public void NonPartialContainer_PrivateViewModel()
		=> Assert.IsNull(GetBindable("Given_ViewModel_Then_GenerateBindable_NonPartialContainer.Private_ViewModel"));

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
	public void NestedFlaggedCodeGen_NotSuffixedTest()
		=> Assert.IsNotNull(GetBindable(typeof(NestedFlaggedCodeGen_NotSuffixed)));

	[TestMethod]
	public void NestedInternalFlaggedCodeGen_NotSuffixedTest()
		=> Assert.IsNotNull(GetBindable(typeof(NestedInternalFlaggedCodeGen_NotSuffixed)));

	[TestMethod]
	public void NestedPrivateFlaggedCodeGenView_NotSuffixedViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(NestedPrivateFlaggedCodeGen_NotSuffixed)));

	[TestMethod]
	public void InheritingViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(SubViewModel)));

	[TestMethod]
	public void NestedInheritingViewModel()
		=> Assert.IsNotNull(GetBindable(typeof(NestedSubViewModel)));

	private Type? GetBindable(Type vmType)
		=> GetBindable(vmType.FullName!);


	private Type? GetBindable(string vmType)
	{
		vmType = TrimEnd(vmType, "Model", StringComparison.Ordinal);

		var index = vmType.LastIndexOf('+');
		if (index >= 0)
		{
			return GetType().Assembly.GetType($"{vmType.Substring(0, index)}+{vmType.Substring(index + 1)}ViewModel");
		}

		index = vmType.LastIndexOf('.');
		if (index >= 0)
		{
			return GetType().Assembly.GetType($"{vmType.Substring(0, index)}.{vmType.Substring(index + 1)}ViewModel");
		}

		return GetType().Assembly.GetType($"{vmType}ViewModel");
	}

	private string TrimEnd(string text, string value, StringComparison comparison)
		=> text.EndsWith(value, comparison)
			? text.Substring(0, text.Length - value.Length)
			: text;

	public partial class Nested_ViewModel { }

	internal partial class NestedInternal_ViewModel { }

	private partial class NestedPrivate_ViewModel { }
	public class NestedNonPartial_ViewModel { }

	[ReactiveBindable(false)]
	public partial class NestedFlaggedNoCodeGen_ViewModel { }

	[ReactiveBindable(false)]
	internal partial class NestedInternalFlaggedNoCodeGen_ViewModel { }

	[ReactiveBindable(false)]
	private partial class NestedPrivateFlaggedNoCodeGen_ViewModel { }

	[ReactiveBindable(true)]
	public partial class NestedFlaggedCodeGen_NotSuffixed { }

	[ReactiveBindable(true)]
	internal partial class NestedInternalFlaggedCodeGen_NotSuffixed { }

	[ReactiveBindable(true)]
	private partial class NestedPrivateFlaggedCodeGen_NotSuffixed { }

	private partial class NestedBaseViewModel
	{
		public NestedBaseViewModel(string parameter) { }
	}

	private partial class NestedSubViewModel : NestedBaseViewModel
	{
		public NestedSubViewModel(string parameter) : base (parameter) { }
		public NestedSubViewModel() : base("42") { }
	}
}

public partial class Given_ViewModel_Then_GenerateBindable_ViewModel { }

[ReactiveBindable(false)]
public partial class Given_ViewModel_Then_GenerateBindable_FlaggedNoCodeGen_ViewModel { }

[ReactiveBindable(true)]
public partial class Given_ViewModel_Then_GenerateBindable_FlaggedCodeGen_NotSuffixed { }

public class Given_ViewModel_Then_GenerateBindable_NonPartial_ViewModel { }

public partial record Given_ViewModel_Then_GenerateBindable_Record_ViewModel { }

public record Given_ViewModel_Then_GenerateBindable_RecordNonPartial_ViewModel { }

public class Given_ViewModel_Then_GenerateBindable_NonPartialContainer
{
	public class Public_ViewModel { }
	internal class Internal_ViewModel { }
	private class Private_ViewModel { }
}

public partial class BaseViewModel
{
	public BaseViewModel(string parameter) { }
}

public partial class SubViewModel : BaseViewModel
{
	public SubViewModel(string parameter) : base(parameter) { }
	public SubViewModel() : base("42") { }
}
