using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A feed which renders a plain object as a single feed message — used by the FeedView to accept
/// mock data (POCOs, anonymous objects, dictionaries built from JSON) as its Source.
/// </summary>
/// <remarks>
/// The source object can either be a raw value (wrapped as-is in the Some state), or a mock "envelope"
/// which drives the feed axes explicitly. An object is an envelope only when at least one of its public
/// readable properties (or dictionary keys) is a recognized envelope member — <c>Data</c>,
/// <c>Progress</c> (aliases <c>IsProgress</c> / <c>InProgress</c>), <c>Error</c> (alias
/// <c>Exception</c>) — and **all** of its members are within that set. Matching is case-insensitive.
/// See specs/009-feedview-mock-source/spec.md.
/// </remarks>
internal sealed class MockFeed : IFeed<object>
{
	private readonly Option<object> _data;
	private readonly Exception? _error;
	private readonly bool _isTransient;

	/// <summary>
	/// Coerces an arbitrary object into a feed suitable for the FeedView.
	/// </summary>
	/// <param name="source">The object to coerce.</param>
	/// <returns>
	/// The <paramref name="source"/> itself when it is already a feed, the feed found in an envelope's
	/// <c>Data</c> member, or a <see cref="MockFeed"/> wrapping the object.
	/// </returns>
	public static ISignal<IMessage> Create(object source)
		=> source as ISignal<IMessage>
			?? AttachedProperty.GetOrCreate(source, typeof(MockFeed), static (src, _) => CreateCore(src));

	private static ISignal<IMessage> CreateCore(object source)
		=> TryCreateFromEnvelope(source) ?? new MockFeed(Option.Some(source), error: null, isTransient: false);

	private MockFeed(Option<object> data, Exception? error, bool isTransient)
	{
		_data = data;
		_error = error;
		_isTransient = isTransient;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<object>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		var subject = new AsyncEnumerableSubject<Message<object>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var refreshGate = new object();
		var refreshToken = RefreshToken.Initial(this, context);

		var current = (Message<object>)Message<object>.Initial
			.With()
			.Data(_data)
			.Error(_error)
			.IsTransient(_isTransient);
		subject.SetNext(current);

		// A mock has nothing to reload, but we still have to honor refresh requests: the FeedView's
		// Refresh command completes only once a message carrying the requested token is received.
		// We re-emit the same mocked axes stamped with the token.
		context.Requests<RefreshRequest>(
			request =>
			{
				lock (refreshGate)
				{
					var token = RefreshToken.InterlockedIncrement(ref refreshToken);
					request.Register(token);
					current = current
						.With()
						.Data(_data)
						.Error(_error)
						.IsTransient(_isTransient) // Transient axes are cleared by With(), re-apply the mocked progress.
						.Refreshed(token);
					subject.TrySetNext(current);
				}
			},
			ct);

		// Registered last so that, on a request-less (None) context which raises EndRequest at
		// subscription, the initial message above is already enqueued before we complete.
		context.Requests<EndRequest>(_ => subject.TryComplete(), ct);

		return subject;
	}

	private static ISignal<IMessage>? TryCreateFromEnvelope(object source)
	{
		if (source is IDictionary<string, object?> dictionary)
		{
			return TryCreateFromDictionary(dictionary);
		}

#pragma warning disable IL2026 // Envelope detection degrades gracefully: if property metadata was trimmed, the object is wrapped whole as a plain value (documented; dictionary sources are the trim-safe path).
		return TryCreateFromProperties(source);
#pragma warning restore IL2026
	}

	private static ISignal<IMessage>? TryCreateFromDictionary(IDictionary<string, object?> source)
	{
		var envelope = new Envelope();
		foreach (var member in source)
		{
			if (!envelope.TryAdd(member.Key, member.Value))
			{
				return null;
			}
		}

		return envelope.TryBuild();
	}

	[RequiresUnreferencedCode("Inspects the public properties of the mock source object; under trimming their metadata may have been removed, in which case the object is treated as a plain value. Prefer IDictionary<string, object?> sources for trimmed apps.")]
	private static ISignal<IMessage>? TryCreateFromProperties(object source)
	{
		var envelope = new Envelope();
		foreach (var property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (!property.CanRead || property.GetIndexParameters().Length > 0)
			{
				continue;
			}

			if (!envelope.TryAdd(property.Name, property.GetValue(source)))
			{
				return null;
			}
		}

		return envelope.TryBuild();
	}

	/// <summary>
	/// Accumulates recognized envelope members; a single unrecognized member disqualifies the source.
	/// </summary>
	private struct Envelope
	{
		private bool _hasAny;
		private bool _hasData;
		private object? _data;
		private object? _progress;
		private object? _error;

		public bool TryAdd(string name, object? value)
		{
			if (name.Equals("Data", StringComparison.OrdinalIgnoreCase))
			{
				_hasData = true;
				_data = value;
			}
			else if (name.Equals("Progress", StringComparison.OrdinalIgnoreCase)
				|| name.Equals("IsProgress", StringComparison.OrdinalIgnoreCase)
				|| name.Equals("InProgress", StringComparison.OrdinalIgnoreCase))
			{
				_progress = value;
			}
			else if (name.Equals("Error", StringComparison.OrdinalIgnoreCase)
				|| name.Equals("Exception", StringComparison.OrdinalIgnoreCase))
			{
				_error = value;
			}
			else
			{
				return false; // Unrecognized member: this is a plain object, not an envelope.
			}

			_hasAny = true;
			return true;
		}

		public readonly ISignal<IMessage>? TryBuild()
		{
			if (!_hasAny)
			{
				return null;
			}

			// An envelope whose Data is itself a feed is a passthrough: the feed drives the view directly.
			if (_data is ISignal<IMessage> feed)
			{
				return feed;
			}

			var data = _hasData
				? _data is null ? Option<object>.None() : Option.Some(_data)
				: Option<object>.Undefined();

			return new MockFeed(data, CoerceError(_error), CoerceProgress(_progress));
		}

		private static Exception? CoerceError(object? value)
			=> value switch
			{
				null => null,
				Exception error => error,
				// JSON cannot carry an Exception: any other value (typically a string) becomes the error message.
				_ => new MockFeedException(value.ToString() ?? value.GetType().Name),
			};

		private static bool CoerceProgress(object? value)
			=> value switch
			{
				null => false,
				bool isInProgress => isInProgress,
				// Covers "true" strings and JSON booleans surfaced as JsonElement, without a System.Text.Json dependency.
				_ => bool.TryParse(value.ToString(), out var parsed) && parsed,
			};
	}
}
