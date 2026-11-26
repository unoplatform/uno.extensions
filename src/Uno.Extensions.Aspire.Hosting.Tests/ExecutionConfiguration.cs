using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Uno.Extensions.Aspire.Hosting.Tests;

internal static class ExecutionConfiguration
{
	/// <summary>
	/// Resolves a resource's arguments and environment as a start does, without starting a process.
	/// </summary>
	public static Task<IExecutionConfigurationResult> ResolveAsync(IDistributedApplicationBuilder builder, IResource resource, ILogger? logger = null) =>
		ExecutionConfigurationBuilder.Create(resource)
			.WithArgumentsConfig()
			.WithEnvironmentVariablesConfig()
			.BuildAsync(builder.ExecutionContext, logger ?? NullLogger.Instance, CancellationToken.None);
}
