using System;
using System.Collections.Generic;
using System.IO;

namespace Uno.Extensions.Reactive.Generator.Compat;

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
		if (_context.NotNullIfNotNullAttribute is null && !GetIsDisabled("UnoExtensionsGeneration_DisableNotNullIfNotNullAttribute"))
		{
			yield return (nameof(_context.NotNullIfNotNullAttribute), GetNotNullIfNotNullAttribute());
		}
		if (_context.NotNullWhenAttribute is null && !GetIsDisabled("UnoExtensionsGeneration_DisableNotNullWhenAttribute"))
		{
			yield return (nameof(_context.NotNullWhenAttribute), GetNotNullWhenAttribute());
		}
		if (_context.MemberNotNullWhenAttribute is null && !GetIsDisabled("UnoExtensionsGeneration_DisableMemberNotNullWhenAttribute"))
		{
			yield return (nameof(_context.MemberNotNullWhenAttribute), GetMemberNotNullWhenAttribute());
		}
		if (_context.IsExternalInit is null && !GetIsDisabled("UnoExtensionsGeneration_DisableIsExternalInit"))
		{
			yield return (nameof(_context.IsExternalInit), GetIsExternalInit());
		}
		if (_context.ModuleInitializerAttribute is null && !GetIsDisabled("UnoExtensionsGeneration_DisableModuleInitializerAttribute"))
		{
			yield return (nameof(_context.ModuleInitializerAttribute), GetModuleInitializerAttribute());
		}
	}

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

	private string GetMemberNotNullWhenAttribute()
		=> $@"{this.GetFileHeader(3)}

			using global::System;

			namespace System.Diagnostics.CodeAnalysis
			{{
				/// <summary>Specifies that the method or property will ensure that the listed field and property members have non-null values when returning with the specified return value condition.</summary>
				[global::System.AttributeUsage(global::System.AttributeTargets.Method | global::System.AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
				public sealed class MemberNotNullWhenAttribute : global::System.Attribute
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
