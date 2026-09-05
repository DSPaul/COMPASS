using Autofac;

namespace COMPASS.Common.DependencyInjection;

/// <summary>
/// Marks a class as a factory. Classes with this attribute are
/// automatically registered as self in the container.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FactoryAttribute : Attribute
{ }

public static class FactoryRegistrar
{
    /// <summary>
    /// Registers every [Factory]-attributed class in the COMPASS.Common assembly as self.
    /// </summary>
    public static void RegisterFactories(this ContainerBuilder builder)
    {
        var factoryTypes = typeof(FactoryAttribute).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsDefined(typeof(FactoryAttribute), false));

        foreach (Type factoryType in factoryTypes)
        {
            builder.RegisterType(factoryType).AsSelf();
        }
    }
}
