namespace Uno.Extensions;

public static class ObjectExtensions
{
    public static TInstance? Get<TInstance>(this object entity)
    {
        if (entity is IInstance<TInstance> instanceEntity)
        {
            return instanceEntity.Instance;
        }

        return default;
    }
}
