﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rocket.Surgery.Extensions.Marten.AspNetCore;
using Rocket.Surgery.Extensions.Marten;
using Rocket.Surgery.Extensions.Marten.Builders;
using Rocket.Surgery.Conventions;
using Rocket.Surgery.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;
using Rocket.Surgery.Extensions.Marten.Functions;

// ReSharper disable once CheckNamespace
namespace Rocket.Surgery.Conventions
{
    /// <summary>
    /// Class MartenFunctionsUnitOfWorkConventionExtensions.
    /// </summary>
    public static class MartenFunctionsUnitOfWorkConventionExtensions
    {
        /// <summary>
        /// Adds the marten functions unit of work.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>IConventionHostBuilder.</returns>
        public static IConventionHostBuilder AddMartenFunctionsUnitOfWork(this IConventionHostBuilder builder)
        {
            builder.Scanner.AppendConvention(new MartenFunctionsUnitOfWorkConvention());
            return builder;
        }
    }
}
