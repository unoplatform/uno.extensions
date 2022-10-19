using System;

namespace Uno.Extensions.Edition;

/// <summary>
/// A property selector for which a <see cref="IValueAccessor{TEntity,TValue}"/> will be generated at compile-time.
/// </summary>
/// <typeparam name="TEntity">Type of the owning entity.</typeparam>
/// <typeparam name="TValue">Type of the value of the property.</typeparam>
/// <param name="entity">The owning entity.</param>
/// <returns>The value of the property.</returns>
/// <remarks>Property selectors can only be of the form `e => e.A.B.C`, you cannot use method nor external value (i.e. cannot have any closure), and cannot be a method group.</remarks>
/// <remarks>See https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/PropertySelector/concept.html for more details.</remarks>
public delegate TValue PropertySelector<in TEntity, out TValue>(TEntity entity);
