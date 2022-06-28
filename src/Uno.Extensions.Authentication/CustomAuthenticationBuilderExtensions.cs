﻿namespace Uno.Extensions.Authentication;

public static class CustomAuthenticationBuilderExtensions
{
	public static ICustomAuthenticationBuilder Login(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher, ITokenCache, IDictionary<string, string>, bool> loginCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				LoginCallback = loginCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder Refresh(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, bool> refreshCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				RefreshCallback = refreshCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder Logout(
		this ICustomAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDispatcher, ITokenCache, bool> logoutCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				LogoutCallback = logoutCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder<TService> Login<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDispatcher, ITokenCache, IDictionary<string, string>, bool> loginCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings< TService >> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				LoginCallback = loginCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder<TService> Refresh<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, ITokenCache, bool> refreshCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings<TService>> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				RefreshCallback = refreshCallback
			};
		}

		return builder;
	}

	public static ICustomAuthenticationBuilder<TService> Logout<TService>(
		this ICustomAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDispatcher, ITokenCache, bool> logoutCallback)
	{
		if (builder is IBuilder<CustomAuthenticationSettings<TService>> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				LogoutCallback = logoutCallback
			};
		}

		return builder;
	}
}
