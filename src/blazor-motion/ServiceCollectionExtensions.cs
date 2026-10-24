using BlazorMotion.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace BlazorMotion;

/// <summary>
/// Registers BlazorMotion services with the dependency injection container.
/// </summary>
public static class BlazorMotionServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMotionInterop"/> and the default <see cref="MotionOptions"/>.
    /// Safe to call more than once.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddBlazorMotion(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddBlazorMotionCore(services, configure: null);
    }

    /// <summary>
    /// Registers <see cref="IMotionInterop"/> and applies <paramref name="configure"/> to <see cref="MotionOptions"/>.
    /// Safe to call more than once.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Customizes the global BlazorMotion options. Validated when the options are first resolved.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <c>null</c>.</exception>
    public static IServiceCollection AddBlazorMotion(this IServiceCollection services, Action<MotionOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return AddBlazorMotionCore(services, configure);
    }

    private static IServiceCollection AddBlazorMotionCore(IServiceCollection services, Action<MotionOptions>? configure)
    {
        // Scoped: one interop bridge per Blazor Server circuit. Under WebAssembly the scope is the
        // whole application, so this behaves like a singleton there.
        services.TryAddScoped<IMotionInterop, MotionInterop>();

        services.AddOptions<MotionOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<MotionOptions>, MotionOptionsValidator>());

        return services;
    }
}
