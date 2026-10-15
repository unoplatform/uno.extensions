using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;

namespace Uno.Extensions.Reactive.Tests;

/// <summary>
/// Helpers shared by the tests of the MVUX validation (states, axis, bindables and commands).
/// </summary>
internal static class ValidationTestHelper
{
	private const int Attempts = 500;
	private const int DelayMs = 10;

	/// <summary>
	/// Waits until the <paramref name="predicate"/> is true, throwing a <see cref="TimeoutException"/> after ~5 seconds.
	/// </summary>
	public static async Task WaitFor(Func<bool> predicate)
	{
		for (var i = 0; i < Attempts; i++)
		{
			if (predicate())
			{
				return;
			}

			await Task.Delay(DelayMs);
		}

		throw new TimeoutException();
	}

	/// <summary>
	/// Waits until the async <paramref name="predicate"/> is true, throwing a <see cref="TimeoutException"/> after ~5 seconds.
	/// </summary>
	public static async Task WaitForAsync(Func<Task<bool>> predicate)
	{
		for (var i = 0; i < Attempts; i++)
		{
			if (await predicate())
			{
				return;
			}

			await Task.Delay(DelayMs);
		}

		throw new TimeoutException();
	}
}

/// <summary>
/// A model validated using DataAnnotations, whose error message is a resource key.
/// </summary>
public sealed class ValidatedPerson
{
	[Required(ErrorMessage = "Validation_NameRequired")]
	public string? Name { get; init; }
}

/// <summary>
/// An in-memory <see cref="IStringLocalizer"/>, which counts its lookups.
/// </summary>
internal sealed class TestLocalizer : IStringLocalizer, IEnumerable<KeyValuePair<string, string>>
{
	private readonly ConcurrentDictionary<string, string> _resources = new();
	private int _lookups;

	public int Lookups => _lookups;

	public void Add(string name, string value)
		=> _resources[name] = value;

	public LocalizedString this[string name]
	{
		get
		{
			Interlocked.Increment(ref _lookups);
			return _resources.TryGetValue(name, out var value)
				? new LocalizedString(name, value)
				: new LocalizedString(name, name, resourceNotFound: true);
		}
	}

	public LocalizedString this[string name, params object[] arguments]
		=> new(name, string.Format(CultureInfo.CurrentCulture, this[name].Value, arguments));

	public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
		=> _resources.Select(kvp => new LocalizedString(kvp.Key, kvp.Value));

	IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
		=> _resources.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> _resources.GetEnumerator();
}
