using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Reactive.Commands;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Validation;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Extensions to configure commands using the <see cref="ICommandBuilder{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// The validation runs each time the command is executed, right before the action configured with Then, while the command is executing.
/// The results are published on the <see cref="MessageAxis.Validation"/> of the state configured as parameter of the command (using Given),
/// and if there is any error, the execution is aborted: the action is not invoked, and the execution completes without error
/// (i.e. the ExecutionCompleted event is raised the same way as for a successful execution).
/// </para>
/// <para>
/// The results are not cleared when the value of the state is changed: they are replaced on the next execution of the command
/// (which also replaces the results of a validator configured on the state itself using State.Validate, and vice versa).
/// Like any update of a state, they are however cleared if the source of the state produces a new value (e.g. a refresh of a State.Async).
/// </para>
/// <para>
/// If the parameter is not a state (e.g. a feed, or a parameter provided by the view), the execution is still aborted, but the results are only logged.
/// The validation does not alter the CanExecute of the command, so the user can always retry.
/// If the validator throws, the execution fails (the error is reported to the error handler of the command) and the previous results are kept.
/// The validator should honor its cancellation token (cancelled when the command is disposed) and apply its own timeout if it might hang (e.g. a remote validation),
/// as the command remains executing (and cannot be executed again with the same parameter) until the validator completes.
/// </para>
/// <para>
/// Configuring the validation multiple times on the same command replaces the previous validation (the last one wins).
/// </para>
/// </remarks>
public static class CommandBuilderExtensions
{
	/// <summary>
	/// Validates the parameter of the command each time the command is executed, aborting the execution if it is not valid.
	/// </summary>
	/// <typeparam name="T">Type of the parameter.</typeparam>
	/// <param name="builder">The command builder.</param>
	/// <param name="validator">The async method which validates the parameter.</param>
	/// <param name="localizer">An optional localizer: when provided, the <see cref="ValidationResult.ErrorMessage"/> of the results are resource keys resolved through it.</param>
	/// <returns>The command builder to complete fluent configuration.</returns>
	/// <remarks>See the remarks of <see cref="CommandBuilderExtensions"/>.</remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="builder"/> has not been created using the <see cref="Command"/> factories.</exception>
	public static IConditionalCommandBuilder<T> Validation<T>(
		this ICommandBuilder<T> builder,
		Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator,
		IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(validator);

		return AddValidation(builder, validator, localizer);
	}

	/// <summary>
	/// Validates the parameter of the command each time the command is executed, using a validator which returns the error message of an invalid parameter.
	/// </summary>
	/// <typeparam name="T">Type of the parameter.</typeparam>
	/// <param name="builder">The command builder.</param>
	/// <param name="validator">The async method which validates the parameter, returning the error message (or its resource key when a <paramref name="localizer"/> is provided), or null (or empty) when the parameter is valid.</param>
	/// <param name="localizer">An optional localizer: when provided, the value returned by the <paramref name="validator"/> is a resource key resolved through it.</param>
	/// <returns>The command builder to complete fluent configuration.</returns>
	/// <remarks>The error is reported for the parameter itself (its <see cref="ValidationResult.MemberNames"/> is empty). See the remarks of <see cref="CommandBuilderExtensions"/>.</remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="builder"/> has not been created using the <see cref="Command"/> factories.</exception>
	public static IConditionalCommandBuilder<T> Validation<T>(this ICommandBuilder<T> builder, AsyncFunc<T, string?> validator, IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(validator);

		return AddValidation(builder, ValidationHelper.FromErrorMessage(validator), localizer);
	}

	/// <summary>
	/// Validates the parameter of the command each time the command is executed, using a predicate and a fixed error.
	/// </summary>
	/// <typeparam name="T">Type of the parameter.</typeparam>
	/// <param name="builder">The command builder.</param>
	/// <param name="isValid">The async predicate which returns true when the parameter is valid.</param>
	/// <param name="error">The error message (or its resource key when a <paramref name="localizer"/> is provided) reported when the parameter is not valid.</param>
	/// <param name="localizer">An optional localizer: when provided, <paramref name="error"/> is a resource key resolved through it.</param>
	/// <returns>The command builder to complete fluent configuration.</returns>
	/// <remarks>The error is reported for the parameter itself (its <see cref="ValidationResult.MemberNames"/> is empty). See the remarks of <see cref="CommandBuilderExtensions"/>.</remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="builder"/> has not been created using the <see cref="Command"/> factories.</exception>
	public static IConditionalCommandBuilder<T> Validation<T>(this ICommandBuilder<T> builder, AsyncFunc<T, bool> isValid, string error, IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(isValid);
		ArgumentException.ThrowIfNullOrEmpty(error);

		return AddValidation(builder, ValidationHelper.FromPredicate(isValid, error), localizer);
	}

	/// <summary>
	/// Validates the parameter of the command each time the command is executed, using an <see cref="IValidator"/>.
	/// </summary>
	/// <typeparam name="T">Type of the parameter.</typeparam>
	/// <param name="builder">The command builder.</param>
	/// <param name="validator">The validator to use (e.g. the one registered by <c>UseValidation</c>).</param>
	/// <param name="localizer">An optional localizer: when provided, the <see cref="ValidationResult.ErrorMessage"/> of the results are resource keys resolved through it.</param>
	/// <returns>The command builder to complete fluent configuration.</returns>
	/// <remarks>
	/// A null parameter has nothing to validate: it is considered as valid (i.e. the action is invoked with null).
	/// See the remarks of <see cref="CommandBuilderExtensions"/>.
	/// </remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="builder"/> has not been created using the <see cref="Command"/> factories.</exception>
	public static IConditionalCommandBuilder<T> Validation<T>(this ICommandBuilder<T> builder, IValidator validator, IStringLocalizer? localizer = null)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(validator);

		return AddValidation(builder, ValidationHelper.FromValidator<T>(validator), localizer);
	}

	/// <inheritdoc cref="Validation{T}(ICommandBuilder{T}, Func{T, CancellationToken, ValueTask{IEnumerable{ValidationResult}}}, IStringLocalizer)"/>
	public static IConditionalCommandBuilder<T> Validation<T>(
		this IConditionalCommandBuilder<T> builder,
		Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator,
		IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(validator);

		return AddValidation(builder, validator, localizer);
	}

	/// <inheritdoc cref="Validation{T}(ICommandBuilder{T}, AsyncFunc{T, string}, IStringLocalizer)"/>
	public static IConditionalCommandBuilder<T> Validation<T>(this IConditionalCommandBuilder<T> builder, AsyncFunc<T, string?> validator, IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(validator);

		return AddValidation(builder, ValidationHelper.FromErrorMessage(validator), localizer);
	}

	/// <inheritdoc cref="Validation{T}(ICommandBuilder{T}, AsyncFunc{T, bool}, string, IStringLocalizer)"/>
	public static IConditionalCommandBuilder<T> Validation<T>(this IConditionalCommandBuilder<T> builder, AsyncFunc<T, bool> isValid, string error, IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(isValid);
		ArgumentException.ThrowIfNullOrEmpty(error);

		return AddValidation(builder, ValidationHelper.FromPredicate(isValid, error), localizer);
	}

	/// <inheritdoc cref="Validation{T}(ICommandBuilder{T}, IValidator, IStringLocalizer)"/>
	public static IConditionalCommandBuilder<T> Validation<T>(this IConditionalCommandBuilder<T> builder, IValidator validator, IStringLocalizer? localizer = null)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(validator);

		return AddValidation(builder, ValidationHelper.FromValidator<T>(validator), localizer);
	}

	private static IConditionalCommandBuilder<T> AddValidation<T>(
		object builder,
		Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator,
		IStringLocalizer? localizer)
	{
		ArgumentNullException.ThrowIfNull(builder);
		if (builder is not IValidatingCommandBuilder validating)
		{
			throw new NotSupportedException($"Validation is supported only on command builders provided by the Command factories (got '{builder.GetType().Name}').");
		}

		var localized = validator.Localized(localizer);

		// Note: The builder is covariant, so we work with object parameter to support a builder of a derived type (e.g. ICommandBuilder<object> for a CommandBuilder<string>).
		return (IConditionalCommandBuilder<T>)validating.Validation((parameter, ct) => localized((T)parameter!, ct));
	}
}
