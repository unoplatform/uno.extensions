namespace Uno.Extensions;

public interface IInjectable<T>
{
    void Inject(T entity);
}
