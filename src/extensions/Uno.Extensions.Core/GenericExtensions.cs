using System;
using System.Diagnostics;

namespace Uno.Extensions;

// This was renamed to GenericExtensions because ObjectExtensions clashed
// with a type in the Uno.Core library
public static class GenericExtensions
{
    public static TInstance? Get<TInstance>(this object entity)
    {
        if (entity is IInstance<TInstance> instanceEntity)
        {
            return instanceEntity.Instance;
        }

        return default;
    }

    [Conditional("DEBUG")]
    public static void AssertNotNull<T>(T entity, string argumentName)
    {
        if (entity is null)
        {
            throw new ArgumentNullException(argumentName);
        }
    }
}
