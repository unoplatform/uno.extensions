namespace Uno.Extensions.Diagnostics;

/// <summary>
/// Defines static methods for starting/stopping timers for measuring performance
/// </summary>
public static class PerformanceTimer
{
	/// <summary>
	/// Conditiona compliation symbol to enable performance timers for internal
	/// extensions logic. Define constant in Directory.Build.props to enable
	/// performance timers eg
	/// <DefineConstants>$(DefineConstants);UNO_EXT_TIMERS</DefineConstants>
	/// Call InitializeTimers from App.xaml.cs, then call Start and Stop
	/// to control timers for different scenarios
	/// </summary>
	public const string ConditionalSymbol = "UNO_EXT_TIMERS";

	private static IDictionary<Guid, Stopwatch> Timers = new Dictionary<Guid, Stopwatch>();

	private static Action<ILogger, LogLevel, Guid, string> _startAction = static (logger, level, key, caller) => { };
	private static Func<Guid, TimeSpan> _splitAction = static (key) => TimeSpan.Zero;
	private static Func<ILogger, LogLevel, Guid, string, TimeSpan> _stopAction = static (logger, level, key, caller) => TimeSpan.Zero;

	/// <summary>
	/// Initializes performance timing methods. Make sure you also define the
	/// UNO_EXT_TIMERS constant to prevent this method from being removed during compilation
	/// </summary>
	[Conditional(ConditionalSymbol)]
	public static void InitializeTimers()
	{
		_startAction = InternalStart;
		_splitAction = InternalSplit;
		_stopAction = InternalStop;
	}

	/// <summary>
	/// Start a timer
	/// </summary>
	/// <param name="logger">Logger to output start message to</param>
	/// <param name="level">LogLevel to output start message at</param>
	/// <param name="key">Unique identifier for timer</param>
	/// <param name="caller">The method calling the start method</param>
	public static void Start(ILogger logger, LogLevel level, Guid key, [CallerMemberName] string caller = "") => _startAction(logger, level, key, caller);

	/// <summary>
	/// Returns an in-progress timespan for the specified timer
	/// </summary>
	/// <param name="key">The unique identifier for the timer</param>
	/// <returns>Elapsed time since timer was started</returns>
	public static TimeSpan Split(Guid key) => _splitAction(key);

	/// <summary>
	/// Stops a timer
	/// </summary>
	/// <param name="logger">Logger to output stop message to</param>
	/// <param name="level">LogLevel to output stop message at</param>
	/// <param name="key">Unique identifier for timer</param>
	/// <param name="caller">The method calling the stop method</param>
	/// <returns>Elapsed time since timer was started</returns>
	public static TimeSpan Stop(ILogger logger, LogLevel level, Guid key, [CallerMemberName] string caller = "") => _stopAction(logger, level, key, caller);


	private static void InternalStart(ILogger logger, LogLevel level, Guid key, [CallerMemberName] string caller = "")
	{
		var timer = new Stopwatch();
		timer.Start();
		Timers[key] = timer;
		if (logger?.IsEnabled(level) ?? false)
		{
			logger.Log(level, $"[{key}:{caller}] Start");
		}
	}


	private static TimeSpan InternalSplit(Guid key)
	{
		if (Timers.TryGetValue(key, out var timer))
		{
			return timer.Elapsed;
		}
		else
		{
			return TimeSpan.Zero;
		}
	}

	private static TimeSpan InternalStop(ILogger logger, LogLevel level, Guid key, [CallerMemberName] string caller = "")
	{
		if (Timers.TryGetValue(key, out var timer))
		{
			Timers.Remove(key);
			timer.Stop();
			if (logger?.IsEnabled(level) ?? false)
			{
				logger.Log(level, $"[{key}:{caller}] {timer.ElapsedMilliseconds}ms");
			}
			return timer.Elapsed;
		}
		else
		{
			if (logger?.IsEnabled(level) ?? false)
			{
				logger.Log(level, $"[{key}:{caller}] MISSING - Either Start hasn't been called, or Stop has already been called");
			}
			return TimeSpan.Zero;
		}

	}
}
