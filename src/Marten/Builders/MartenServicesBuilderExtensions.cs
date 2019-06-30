using System;
using System.Collections.Generic;
using Baseline;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Rocket.Surgery.Extensions.Marten.Builders
{
    /// <summary>
    /// Class MartenServicesBuilderExtensions.
    /// </summary>
    public static class MartenServicesBuilderExtensions
    {
        /// <summary>
        /// Configures the specified action.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="action">The action.</param>
        /// <returns>MartenServicesBuilder.</returns>
        public static MartenServicesBuilder Configure(this MartenServicesBuilder builder, MartenConfigurationDelegate action)
        {
            builder.AddDelegate(action);
            return builder;
        }

        /// <summary>
        /// Configures the specified action.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="action">The action.</param>
        /// <returns>MartenServicesBuilder.</returns>
        public static MartenServicesBuilder Configure(this MartenServicesBuilder builder, MartenComponentConfigurationDelegate action)
        {
            builder.AddDelegate(action);
            return builder;
        }

        /// <summary>
        /// Uses the connection string.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>MartenServicesBuilder.</returns>
        public static MartenServicesBuilder UseConnectionString(this MartenServicesBuilder builder, string connectionString)
        {
            return Configure(builder, _ => _.Connection(connectionString));
        }

        /// <summary>
        /// Enable lightweight tracking
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>MartenServicesBuilder.</returns>
        public static MartenServicesBuilder UseLightweightSession(this MartenServicesBuilder builder)
        {
            builder.Parent.Services.RemoveAll<IDocumentSession>();
            builder.Parent.Services.TryAddScoped(c => c.GetRequiredService<IDocumentStore>().LightweightSession());
            return builder;
        }

        /// <summary>
        /// Enable dirty tracking
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>MartenServicesBuilder.</returns>
        public static MartenServicesBuilder UseDirtyTrackedSession(this MartenServicesBuilder builder)
        {
            builder.Parent.Services.RemoveAll<IDocumentSession>();
            builder.Parent.Services.TryAddScoped(c => c.GetRequiredService<IDocumentStore>().DirtyTrackedSession());
            return builder;
        }

#if NETSTANDARD1_6
        /// <summary>
        /// Removes all services of type <typeparamef name="T"/> in <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <returns></returns>
        internal static IServiceCollection RemoveAll<T>(this IServiceCollection collection)
        {
            return RemoveAll(collection, typeof(T));
        }

        /// <summary>
        /// Removes all services of type <paramef name="serviceType"/> in <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="serviceType">The service type to remove.</param>
        /// <returns></returns>
        internal static IServiceCollection RemoveAll(this IServiceCollection collection, Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            for (var i = collection.Count - 1; i >= 0; i--)
            {
                var descriptor = collection[i];
                if (descriptor.ServiceType == serviceType)
                {
                    collection.RemoveAt(i);
                }
            }

            return collection;
        }
#endif
    }
}
