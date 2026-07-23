using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Uno.Extensions.Serialization.Tests")]
// The AOT test project compiles the same test .cs files (Compile Include from ...Serialization.Tests),
// so it also needs access to the internal serializer-options template/factory.
[assembly: InternalsVisibleTo("Uno.Extensions.Serialization.AotTests")]
