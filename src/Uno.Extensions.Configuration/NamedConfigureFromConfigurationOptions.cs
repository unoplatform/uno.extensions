namespace Uno.Extensions.Configuration.Internal;

internal class NamedConfigureFromConfigurationOptions<TOptions> : ConfigureNamedOptions<TOptions>
		where TOptions : class
{
	/// <summary>
	/// Constructor that takes the <see cref="IConfiguration"/> instance to bind against.
	/// </summary>
	/// <param name="name">The name of the options instance.</param>
	/// <param name="config">The <see cref="IConfiguration"/> instance.</param>
	public NamedConfigureFromConfigurationOptions(string? name, IConfiguration config)
		: this(name, config, _ => { })
	{ }

	/// <summary>
	/// Constructor that takes the <see cref="IConfiguration"/> instance to bind against.
	/// </summary>
	/// <param name="name">The name of the options instance.</param>
	/// <param name="config">The <see cref="IConfiguration"/> instance.</param>
	/// <param name="configureBinder">Used to configure the <see cref="BinderOptions"/>.</param>
	public NamedConfigureFromConfigurationOptions(string? name, IConfiguration config, Action<BinderOptions>? configureBinder)
		: base(name, options => config.Bind(options, configureBinder!))
	{
	}
}
