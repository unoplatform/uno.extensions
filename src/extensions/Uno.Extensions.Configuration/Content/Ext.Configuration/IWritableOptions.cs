using System;
using Microsoft.Extensions.Options;

namespace Uno.Extensions.Configuration
{
    public interface IWritableOptions<T> : IOptionsSnapshot<T>
        where T : class, new()
    {
        void Update(Func<T, T> applyChanges);
        void Update(Action<T> applyChanges);
    }
}
