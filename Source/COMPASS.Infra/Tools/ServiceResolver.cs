using Autofac;

namespace COMPASS.Infra.Tools;

public static class ServiceResolver
{
    private static IContainer? _container;
    
    public static void Initialize(IContainer container)
    {
        _container = container;
    }
    
    public static T Resolve<T>() where T : notnull => _container != null
            ? _container.Resolve<T>()
            : throw new Exception("Cannot resolve before it is initialized");
    
    public static T ResolveKeyed<T>(object key) where T : notnull => _container != null
        ? _container.ResolveKeyed<T>(key)
        : throw new Exception("Cannot resolve before it is initialized");
}