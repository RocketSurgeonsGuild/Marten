﻿using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Rocket.Surgery.Extensions.DependencyInjection;
using Rocket.Surgery.Extensions.Marten.Builders;
using Rocket.Surgery.Extensions.Marten.Projections;

namespace Rocket.Surgery.Extensions.Marten
{
    /// <summary>
    /// MartenServicesExtensions.
    /// </summary>
    public static class MartenServicesExtensions
    {
        /// <summary>
        /// Withes the marten.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>MartenServicesBuilder.</returns>
        public static IServiceConventionContext WithMarten(this IServiceConventionContext context)
        {
            DefaultServices(context.Services);

            context.Services.AddOptions();
            context.Services.AddMemoryCache();

            context.Services.TryAddSingleton(new ProjectionDescriptorCollection(context.AssemblyCandidateFinder));

            return context;
        }

        private static void DefaultServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<StoreOptions>, MartenConfigureOptions>()
            );
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<StoreOptions>, MartenRegistryConfigureOptions>()
            );
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<StoreOptions>, MartenProjectionsConfigureOptions>()
            );

            services.TryAddScoped(c => c.GetRequiredService<IDocumentStore>().QuerySession());

            services.TryAddSingleton(_ => new DocumentStore(_.GetRequiredService<IOptions<StoreOptions>>().Value));
            services.TryAddTransient<IDocumentStore, TransientDocumentStore>();
            services.TryAddSingleton<IDaemonFactory, DaemonFactory>();
            services.TryAddTransient(typeof(DaemonLogger<>));
            services.TryAddScoped<IMartenContext, MartenContext>();
        }
    }
}