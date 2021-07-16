using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Uno.Extensions.Configuration
{
    public interface IWritableOptions<T> : IOptionsSnapshot<T>
        where T : class, new()
    {
        Task Update(Func<T, T> applyChanges);

        Task Update(Action<T> applyChanges);
    }
}
