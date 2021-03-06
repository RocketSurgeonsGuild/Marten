﻿using Rocket.Surgery.Extensions.Marten;
using Rocket.Surgery.Extensions.Marten.Functions;

// ReSharper disable once CheckNamespace
namespace Rocket.Surgery.Conventions
{
    /// <summary>
    /// MartenFunctionsUnitOfWorkConventionHostExtensions.
    /// </summary>
    public static class MartenFunctionsUnitOfWorkConventionHostExtensions
    {
        /// <summary>
        /// Adds the marten functions unit of work.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>IConventionHostBuilder.</returns>
        public static IConventionHostBuilder AddMartenUnitOfWorkFunctionFilter(this IConventionHostBuilder builder)
        {
            var options = builder.GetOrAdd(() => new MartenOptions());
            options.AutomaticUnitOfWork = true;
            builder.Scanner.AppendConvention<MartenFunctionsUnitOfWorkConvention>();
            return builder;
        }
    }
}