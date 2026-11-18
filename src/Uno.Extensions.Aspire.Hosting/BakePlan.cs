namespace Aspire.Hosting;

/// <summary>
/// What a head without a process environment receives from the environment Aspire resolved for it.
/// </summary>
/// <param name="Variables">The variables written into the app.</param>
/// <param name="LeftOutServices">Service addresses left out because their value is not made of endpoints only.</param>
/// <param name="LeftOut">Other variables left out.</param>
/// <param name="MissingOptIns">Names opted in that the head does not have.</param>
/// <param name="SecretOptIns">Names opted in whose value holds a secret.</param>
internal sealed record BakePlan(
	IReadOnlyDictionary<string, string> Variables,
	IReadOnlyList<string> LeftOutServices,
	IReadOnlyList<string> LeftOut,
	IReadOnlyList<string> MissingOptIns,
	IReadOnlyList<string> SecretOptIns)
{
	/// <summary>
	/// Service addresses left out because the head does not trust them.
	/// </summary>
	public IReadOnlyList<string> LeftOutUntrusted { get; init; } = [];

	/// <summary>
	/// Services the head gets no address for, because every address was left out as untrusted.
	/// </summary>
	public IReadOnlyList<string> UnreachableServices { get; init; } = [];
}
