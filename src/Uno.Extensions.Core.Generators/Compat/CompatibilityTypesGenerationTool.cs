using System;
using System.Collections.Generic;
using System.IO;

namespace Uno.Extensions.Generators.CompatibilityTypes;

internal class CompatibilityTypesGenerationTool : ICodeGenTool
{
	private readonly CompatibilityTypesGenerationContext _context;

	/// <inheritdoc />
	public string Version => "1";

	public CompatibilityTypesGenerationTool(CompatibilityTypesGenerationContext context)
	{
		_context = context;
	}

	public IEnumerable<(string fileName, string code)> Generate()
	{
		var assembly = _context.Context.Compilation.Assembly;

		// System.Diagnostics.CodeAnalysis
		if (!(_context.DynamicallyAccessedMembersAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableDynamicallyAccessedMembersAttribute"))
		{
			yield return (nameof(_context.DynamicallyAccessedMembersAttribute), GetDynamicallyAccessedMembersAttribute());
		}
		if (!(_context.MaybeNullAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableMaybeNullAttribute"))
		{
			yield return (nameof(_context.MaybeNullAttribute), GetMaybeNullAttribute());
		}
		if (!(_context.MaybeNullWhenAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableMaybeNullWhenAttribute"))
		{
			yield return (nameof(_context.MaybeNullWhenAttribute), GetMaybeNullWhenAttribute());
		}
		if (!(_context.MemberNotNullWhenAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableMemberNotNullAttribute"))
		{
			yield return (nameof(_context.MemberNotNullAttribute), GetMemberNotNullAttribute());
		}
		if (!(_context.MemberNotNullWhenAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableMemberNotNullWhenAttribute"))
		{
			yield return (nameof(_context.MemberNotNullWhenAttribute), GetMemberNotNullWhenAttribute());
		}
		if (!(_context.NotNullIfNotNullAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableNotNullIfNotNullAttribute"))
		{
			yield return (nameof(_context.NotNullIfNotNullAttribute), GetNotNullIfNotNullAttribute());
		}
		if (!(_context.NotNullWhenAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableNotNullWhenAttribute"))
		{
			yield return (nameof(_context.NotNullWhenAttribute), GetNotNullWhenAttribute());
		}

		// System.Reflection.Metadata
		if (!(_context.MetadataUpdateHandlerAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableMetadataUpdateHandlerAttribute"))
		{
			yield return (nameof(_context.MetadataUpdateHandlerAttribute), GetMetadataUpdateHandlerAttribute());
		}

		// System.Runtime.CompilerServices
		if (!(_context.CreateNewOnMetadataUpdateAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableCreateNewOnMetadataUpdateAttribute"))
		{
			yield return (nameof(_context.CreateNewOnMetadataUpdateAttribute), GetCreateNewOnMetadataUpdateAttribute());
		}
		if (!(_context.IsExternalInit?.IsAccessibleTo(assembly, allowInternalsVisibleTo: false) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableIsExternalInit"))
		{
			// Note: about 'allowInternalsVisibleTo: false'
			//		For the compiler to allow 'init' keyword, the IsExternalInit must be either public in a ref assembly, either declared in the current assembly.
			//		This means that it does not allow an internal class that has been made accessible to the current assembly using `InternalsVisibleTo`
			yield return (nameof(_context.IsExternalInit), GetIsExternalInit());
		}
		if (!(_context.MetadataUpdateOriginalTypeAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableMetadataUpdateOriginalTypeAttribute"))
		{
			yield return (nameof(_context.MetadataUpdateOriginalTypeAttribute), GetMetadataUpdateOriginalTypeAttribute());
		}
		if (!(_context.ModuleInitializerAttribute?.IsAccessibleTo(assembly) ?? false) && !GetIsDisabled("UnoExtensionsGeneration_DisableModuleInitializerAttribute"))
		{
			yield return (nameof(_context.ModuleInitializerAttribute), GetModuleInitializerAttribute());
		}
	}

	#region System.Diagnostics.CodeAnalysis
	private string GetDynamicallyAccessedMembersAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableDynamicallyAccessedMembersAttribute>true</UnoExtensionsGeneration_DisableDynamicallyAccessedMembersAttribute>

			namespace System.Diagnostics.CodeAnalysis
			{{
				/// <summary>Indicates that certain members on a specified <see cref=""T:System.Type"" /> are accessed dynamically, for example, through <see cref=""N:System.Reflection"" />.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct | global::System.AttributeTargets.Method | global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Interface | global::System.AttributeTargets.Parameter | global::System.AttributeTargets.ReturnValue | global::System.AttributeTargets.GenericParameter, Inherited = false)]
				internal sealed class DynamicallyAccessedMembersAttribute : global::System.Attribute
				{{
					/// <summary>Initializes a new instance of the <see cref=""T:System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"" /> class with the specified member types.</summary>
					/// <param name=""memberTypes"">The types of the dynamically accessed members.</param>
					public DynamicallyAccessedMembersAttribute(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes memberTypes)
						=> this.MemberTypes = memberTypes;

					/// <summary>Gets the <see cref=""T:System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes"" /> that specifies the type of dynamically accessed members.</summary>
					public global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes MemberTypes {{ get; }}
				}}

				/// <summary>Specifies the types of members that are dynamically accessed.
				/// This enumeration has a <see cref=""T:System.FlagsAttribute"" /> attribute that allows a bitwise combination of its member values.</summary>
				[Flags]
				internal enum DynamicallyAccessedMemberTypes
				{{
					/// <summary>Specifies no members.</summary>
					None = 0,
					/// <summary>Specifies the default, parameterless public constructor.</summary>
					PublicParameterlessConstructor = 1,
					/// <summary>Specifies all public constructors.</summary>
					PublicConstructors = 3,
					/// <summary>Specifies all non-public constructors.</summary>
					NonPublicConstructors = 4,
					/// <summary>Specifies all public methods.</summary>
					PublicMethods = 8,
					/// <summary>Specifies all non-public methods.</summary>
					NonPublicMethods = 16, // 0x00000010
					/// <summary>Specifies all public fields.</summary>
					PublicFields = 32, // 0x00000020
					/// <summary>Specifies all non-public fields.</summary>
					NonPublicFields = 64, // 0x00000040
					/// <summary>Specifies all public nested types.</summary>
					PublicNestedTypes = 128, // 0x00000080
					/// <summary>Specifies all non-public nested types.</summary>
					NonPublicNestedTypes = 256, // 0x00000100
					/// <summary>Specifies all public properties.</summary>
					PublicProperties = 512, // 0x00000200
					/// <summary>Specifies all non-public properties.</summary>
					NonPublicProperties = 1024, // 0x00000400
					/// <summary>Specifies all public events.</summary>
					PublicEvents = 2048, // 0x00000800
					/// <summary>Specifies all non-public events.</summary>
					NonPublicEvents = 4096, // 0x00001000
					/// <summary>Specifies all interfaces implemented by the type.</summary>
					Interfaces = 8192, // 0x00002000
					/// <summary>Specifies all members.</summary>
					All = -1, // 0xFFFFFFFF
				}}
			}}".Align(0);

	private string GetMaybeNullAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableMaybeNullAttribute>true</UnoExtensionsGeneration_DisableMaybeNullAttribute>

			namespace System.Diagnostics.CodeAnalysis
			{{
				/// <summary>Specifies that an output may be <see langword=""null"" /> even if the corresponding type disallows it.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter | global::System.AttributeTargets.ReturnValue, Inherited = false)]
				internal sealed class MaybeNullAttribute : global::System.Attribute
				{{
				}}
			}}".Align(0);

	private string GetMaybeNullWhenAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableMaybeNullWhenAttribute>true</UnoExtensionsGeneration_DisableMaybeNullWhenAttribute>

			namespace System.Diagnostics.CodeAnalysis
			{{
				/// <summary>Specifies that when a method returns <see cref=""P:System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute.ReturnValue"" />, the parameter may be <see langword=""null"" /> even if the corresponding type disallows it.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, Inherited = false)]
				internal sealed class MaybeNullWhenAttribute : global::System.Attribute
				{{
					/// <summary>Initializes the attribute with the specified return value condition.</summary>
					/// <param name=""returnValue"">The return value condition. If the method returns this value, the associated parameter may be <see langword=""null"" />.</param>
					public MaybeNullWhenAttribute(bool returnValue)
						=> this.ReturnValue = returnValue;

					/// <summary>Gets the return value condition.</summary>
					/// <returns>The return value condition. If the method returns this value, the associated parameter may be <see langword=""null"" />.</returns>
					public bool ReturnValue {{ get; }}
				}}
			}}".Align(0);

	public string GetNotNullWhenAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableNotNullWhenAttribute>true</UnoExtensionsGeneration_DisableNotNullWhenAttribute>

			using global::System;

			namespace System.Diagnostics.CodeAnalysis
			{{
				/// <summary>
				/// Specifies that when a method returns <see cref=""ReturnValue""/>, the parameter will not be null even if the corresponding type allows it.
				/// </summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Parameter, Inherited = false)]
				{this.GetCodeGenAttribute()}
				internal class NotNullWhenAttribute : global::System.Attribute
				{{
					/// <summary>
					/// Gets the return value condition.
					/// </summary>
					public bool ReturnValue {{ get; }}

					/// <summary>
					/// The return value condition. If the method returns this value, the associated parameter will not be null.
					/// </summary>
					/// <param name=""returnValue""></param>
					public NotNullWhenAttribute(bool returnValue)
					{{
						ReturnValue = returnValue;
					}}
				}}
			}}
			".Align(0);

	private string GetNotNullIfNotNullAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableNotNullIfNotNullAttribute>true</UnoExtensionsGeneration_DisableNotNullIfNotNullAttribute>

			using global::System;

			namespace System.Diagnostics.CodeAnalysis
			{{
				/// <summary>
				/// Specifies that the output will be non-null if the named parameter is non-null.
				/// </summary>
				{this.GetCodeGenAttribute()}
				[global::System.AttributeUsage(global::System.AttributeTargets.Parameter | global::System.AttributeTargets.Property | global::System.AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
				internal class NotNullIfNotNullAttribute : global::System.Attribute
				{{
					/// <summary>
					/// Gets the associated parameter name.
					/// </summary>
					public string ParameterName {{ get; }}

					/// <summary>
					/// Initializes the attribute with the associated parameter name.
					/// </summary>
					/// <param name=""parameterName"">The associated parameter name. The output will be non-null if the argument to the parameter specified is non-null.</param>
					public NotNullIfNotNullAttribute(string parameterName)
					{{
						ParameterName = parameterName;
					}}
				}}
			}}
			".Align(0);

	private string GetMemberNotNullAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableMemberNotNullAttribute>true</UnoExtensionsGeneration_DisableMemberNotNullAttribute>

			using global::System;

			namespace System.Diagnostics.CodeAnalysis
			{{
				/// <summary>Specifies that the method or property will ensure that the listed field and property members have values that aren't <see langword=""null"" />.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Method | global::System.AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
				internal sealed class MemberNotNullAttribute : global::System.Attribute
				{{
					/// <summary>Initializes the attribute with a field or property member.</summary>
					/// <param name=""member"">The field or property member that is promised to be non-null.</param>
					public MemberNotNullAttribute(string member)
					{{
						Members = new string[1] {{ member }};
					}}

					/// <summary>Initializes the attribute with the list of field and property members.</summary>
					/// <param name=""members"">The list of field and property members that are promised to be non-null.</param>
					public MemberNotNullAttribute(params string[] members)
					{{
						Members = members;
					}}

					/// <summary>Gets field or property member names.</summary>
					public string[] Members {{ get; }}
				}}
			}}
			".Align(0);

	private string GetMemberNotNullWhenAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableMemberNotNullWhenAttribute>true</UnoExtensionsGeneration_DisableMemberNotNullWhenAttribute>

			using global::System;

			namespace System.Diagnostics.CodeAnalysis
			{{
				/// <summary>Specifies that the method or property will ensure that the listed field and property members have non-null values when returning with the specified return value condition.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Method | global::System.AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
				internal sealed class MemberNotNullWhenAttribute : global::System.Attribute
				{{
					/// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
					/// <param name=""returnValue"">The return value condition. If the method returns this value, the associated parameter will not be <see langword=""null"" />.</param>
					/// <param name=""member"">The field or property member that is promised to be non-null.</param>
					public MemberNotNullWhenAttribute(bool returnValue, string member)
					{{
						ReturnValue = returnValue;
						Members = new string[1] {{ member }};
					}}

					/// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
					/// <param name=""returnValue"">The return value condition. If the method returns this value, the associated parameter will not be <see langword=""null"" />.</param>
					/// <param name=""members"">The list of field and property members that are promised to be non-null.</param>
					public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
					{{
						ReturnValue = returnValue;
						Members = members;
					}}

					/// <summary>Gets the return value condition.</summary>
					public bool ReturnValue {{ get; }}

					/// <summary>Gets field or property member names.</summary>
					public string[] Members {{ get; }}
				}}
			}}
			".Align(0);
	#endregion

	#region System.Reflection.Metadata
	private string GetMetadataUpdateHandlerAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableMetadataUpdateHandlerAttribute>true</UnoExtensionsGeneration_DisableMetadataUpdateHandlerAttribute>

			namespace System.Reflection.Metadata
			{{
				/// <summary>Indicates that a type that should receive notifications of metadata updates.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]
				internal sealed class MetadataUpdateHandlerAttribute : global::System.Attribute
				{{
					/// <summary>Initializes the attribute.</summary>
					/// <param name=""handlerType"">A type that handles metadata updates and that should be notified when any occur.</param>
					public MetadataUpdateHandlerAttribute([global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] global::System.Type handlerType)
						=> this.HandlerType = handlerType;

					/// <summary>Gets the type that handles metadata updates and that should be notified when any occur.</summary>
					[global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(global::System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)]
					public global::System.Type HandlerType {{ get; }}
				}}
			}}
			".Align(0);
	#endregion

	#region System.Runtime.CompilerServices
	private string GetCreateNewOnMetadataUpdateAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableCreateNewOnMetadataUpdateAttribute>true</UnoExtensionsGeneration_DisableCreateNewOnMetadataUpdateAttribute>

			namespace System.Runtime.CompilerServices
			{{
				/// <summary>Indicates a type should be replaced rather than updated when applying metadata updates.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = false)]
				internal sealed class CreateNewOnMetadataUpdateAttribute : global::System.Attribute
				{{
				}}
			}}".Align(0);

	private string GetIsExternalInit()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableIsExternalInit>true</UnoExtensionsGeneration_DisableIsExternalInit>

			using global::System;

			namespace System.Runtime.CompilerServices
			{{
				/// <summary>
				/// Reserved to be used by the compiler for tracking metadata. This class should not be used by developers in source code.
				/// </summary>
				{this.GetCodeGenAttribute()}
				internal static class IsExternalInit
				{{
				}}
			}}
			".Align(0);

	private string GetMetadataUpdateOriginalTypeAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableMetadataUpdateOriginalTypeAttribute>true</UnoExtensionsGeneration_DisableMetadataUpdateOriginalTypeAttribute>

			namespace System.Runtime.CompilerServices
			{{
				/// <summary>Emitted by the compiler when a type that's marked with <see cref=""T:System.Runtime.CompilerServices.CreateNewOnMetadataUpdateAttribute"" /> is updated during a hot reload session.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
				internal class MetadataUpdateOriginalTypeAttribute : global::System.Attribute
				{{
					/// <summary>Initializes a new instance of the <see cref=""T:System.Runtime.CompilerServices.MetadataUpdateOriginalTypeAttribute"" /> class.</summary>
					/// <param name=""originalType"">The original type that was updated.</param>
					public MetadataUpdateOriginalTypeAttribute(global::System.Type originalType)
						=> this.OriginalType = originalType;

					/// <summary>Gets the original version of the type that this attribute is attached to.</summary>
					public global::System.Type OriginalType {{ get; }}
				}}
			}}".Align(0);

	private string GetModuleInitializerAttribute()
		=> $@"{this.GetFileHeader(3)}

			// Note: You can disable the generation of this file by setting in your project the property
			//		 <UnoExtensionsGeneration_DisableModuleInitializerAttribute>true</UnoExtensionsGeneration_DisableModuleInitializerAttribute>

			using global::System;

			namespace System.Runtime.CompilerServices
			{{
				/// <summary>
				/// Used to indicate to the compiler that a method should be called in its containing module's initializer.
				/// </summary>
				{this.GetCodeGenAttribute()}
				[global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]
				internal sealed class ModuleInitializerAttribute : global::System.Attribute
				{{
				}}
			}}".Align(0);
	#endregion

	private bool GetIsDisabled(string propertyName)
		=> bool.TryParse(_context.Context.GetMSBuildPropertyValue(propertyName), out var isDisabled) && isDisabled;

	private (string name, string code) GetTemplate(string fileName)
	{
		var assembly = this.GetType().Assembly;
		using var resource = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Compat.Templates.{fileName}.cs");
		if (resource is null)
		{
			throw new InvalidOperationException($"Failed to load template '{fileName}'");
		}
		using var resourceReader = new StreamReader(resource);

		var code = this.GetFileHeader() + Environment.NewLine + resourceReader.ReadToEnd();

		return (fileName, code);
	}
}
