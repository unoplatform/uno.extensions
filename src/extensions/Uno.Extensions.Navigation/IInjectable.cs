namespace Uno.Extensions.Navigation;

public interface IInjectable<T>
{
    void Inject(T entity);
}
