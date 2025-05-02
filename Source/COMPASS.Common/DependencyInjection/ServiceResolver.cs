using Autofac;

namespace COMPASS.Common.DependencyInjection;

public static class ServiceResolver
{
    private static IContainer _container;
    
    public static void Initialize(IContainer container)
    {
        _container = container;
    }
    
    public static T Resolve<T>() where T : notnull => _container.Resolve<T>();
}