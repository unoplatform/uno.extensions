using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Writes the environment of a head that has no process environment where the app's build reads it, before the head is
/// built. See specs/027-aspire-support/spec.md, "Baking the environment".
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
internal static class ClientEnvironment
{
	private const string ClientPackageId = "Uno.Extensions.Aspire";

	// The values hold only while this AppHost runs: removed at its start and stop, and renewed in between, so a build
	// can tell a file left by a killed AppHost, which never reaches its stop.
	internal static void RemoveOutsideTheRun(IDistributedApplicationBuilder builder, UnoHeadResource head) =>
		builder.Eventing.Subscribe<BeforeStartEvent>((@event, _) =>
		{
			// The AppHost's own log: at stop, the dashboard that shows a head's log is going away.
			var logger = @event.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(UnoProjectBuilderExtensions));
			BakedEnvironment.Delete(head, logger);
			var lease = new Timer(_ => BakedEnvironment.RenewLease(head, logger), null, BakedEnvironment.LeaseRenewal, BakedEnvironment.LeaseRenewal);
			@event.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
			{
				lease.Dispose();
				BakedEnvironment.Delete(head, logger);
			});
			return Task.CompletedTask;
		});

	internal static async Task WriteAsync(
		UnoHeadResource head,
		Func<BakePlan, BakePlan> forHead,
		DistributedApplicationExecutionContext executionContext,
		ILogger logger,
		CancellationToken cancellationToken)
	{
		var configuration = await ExecutionConfigurationBuilder.Create(head)
			.WithEnvironmentVariablesConfig()
			.BuildAsync(executionContext, logger, cancellationToken)
			.ConfigureAwait(false);

		// A partial environment would overwrite the last good values.
		if (configuration.Exception is { } failure)
		{
			throw new DistributedApplicationException($"The environment of '{head.Name}' could not be resolved, so nothing was written into the app: {failure.Message}", failure);
		}

		var environment = configuration.EnvironmentVariablesWithUnprocessed.Select(variable =>
		{
			var (name, (unprocessed, processed)) = variable;
			return (Name: name, Unprocessed: (object?)unprocessed, Processed: processed);
		});
		var plan = forHead(BakedEnvironment.Plan(environment, head.ClientEnvironmentNames));
		BakedEnvironment.Write(head.Name, BakedEnvironment.ValuesPath(head), BakedEnvironment.CreateValues(head.Name, plan.Variables));
		LogPlan(logger, head.Name, plan);
	}

	// Without the client package the app's build never reads the values, and nothing else would say so.
	// Called after the head's build, which restores: an earlier restore may still list the package.
	internal static void WarnIfTheAppDoesNotReadIt(ILogger logger, UnoHeadResource head)
	{
		bool? references;
		try
		{
			references = ProjectAssets.References(head.Project, ClientPackageId);
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
		{
			// A restore may be rewriting the file; the check only advises.
			logger.LogDebug(exception, "Whether the app of '{Head}' references {Package} could not be read.", head.Name, ClientPackageId);
			return;
		}

		if (references == false)
		{
			logger.LogWarning(
				"The app of '{Head}' does not reference {Package}, so its build does not read what was written and the app gets no service address. Add the package to '{Project}'.",
				head.Name,
				ClientPackageId,
				head.Project.ProjectPath);
		}
	}

	// Lists are passed as they are: the logger joins them only when the level is enabled.
	private static void LogPlan(ILogger logger, string headName, BakePlan plan)
	{
		logger.LogInformation("Written into the app '{Head}': {Names}.", headName, plan.Variables.Keys);

		if (plan.LeftOutServices.Count > 0)
		{
			logger.LogWarning(
				"Not written into the app '{Head}', because their value is not only service addresses and anyone with the app could read it: {Names}. To write one anyway, add .WithClientEnvironment(\"<name>\") on the head.",
				headName,
				plan.LeftOutServices);
		}

		if (plan.LeftOutUntrusted.Count > 0)
		{
			logger.LogInformation("Not written into the app '{Head}', because it does not trust the development certificate: {Names}.", headName, plan.LeftOutUntrusted);
		}

		if (plan.UnreachableServices.Count > 0)
		{
			logger.LogWarning(
				"The app '{Head}' gets no address for {Services}: only https was offered, which it does not trust. Give them an http endpoint.",
				headName,
				plan.UnreachableServices);
		}

		if (plan.MissingOptIns.Count > 0)
		{
			logger.LogWarning("WithClientEnvironment names variables '{Head}' does not have: {Names}.", headName, plan.MissingOptIns);
		}

		if (plan.SecretOptIns.Count > 0)
		{
			logger.LogWarning("Written into the app '{Head}' although they hold a secret, readable by anyone with the app: {Names}.", headName, plan.SecretOptIns);
		}

		if (plan.LeftOut.Count > 0)
		{
			logger.LogDebug("Not written into the app '{Head}', because anyone with the app can read it: {Names}.", headName, plan.LeftOut);
		}
	}
}
