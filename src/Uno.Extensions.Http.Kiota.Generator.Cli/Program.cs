using System.CommandLine;

namespace Uno.Extensions.Http.Kiota.Generator.Cli;

/// <summary>
/// CLI entry point for the Kiota code generator wrapper.
/// Delegates all argument parsing and generation to <see cref="KiotaGeneratorCommand"/>.
/// </summary>
/// <remarks>
/// <para>
/// Usage: <c>kiota-gen --openapi &lt;path&gt; --output &lt;dir&gt; [options]</c>
/// </para>
/// <para>
/// Exit codes:
/// <list type="bullet">
///   <item><description><c>0</c> — code generation succeeded.</description></item>
///   <item><description><c>1</c> — code generation failed (see stderr for MSBuild-format diagnostics).</description></item>
/// </list>
/// </para>
/// </remarks>
internal static class Program
{
	static async Task<int> Main(string[] args)
	{
		var command = new KiotaGeneratorCommand();
		return await command.InvokeAsync(args);
	}
}
