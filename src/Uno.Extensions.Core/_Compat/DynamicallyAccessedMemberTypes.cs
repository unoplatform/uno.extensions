#if NETSTANDARD2_0 || WINDOWS_UWP || NET461
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies the types of members that are dynamically accessed.
/// This enumeration has a <see cref="T:System.FlagsAttribute" /> attribute that allows a bitwise combination of its member values.</summary>
[Flags]
internal enum DynamicallyAccessedMemberTypes
{
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
}
#endif
