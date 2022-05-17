using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Uno.Extensions.Configuration
{
    public interface IWritableOptions<T> : IOptionsSnapshot<T>
        where T : class, new()
    {
        Task UpdateAsync(Func<T, T> applyChanges);

        Task UpdateAsync(Action<T> applyChanges);
    }
}
