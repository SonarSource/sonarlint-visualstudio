/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using SonarQube.Client.Logging;

namespace SonarQube.Client.Requests
{
    internal class UnversionedRequestFactory : IRequestFactory
    {
        private readonly ILogger logger;

        private readonly Dictionary<Type, Func<IRequest>> requestToFactoryMap;

        public UnversionedRequestFactory(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            requestToFactoryMap = new Dictionary<Type, Func<IRequest>>();
            this.logger = logger;
        }

        /// <summary>
        /// Registers a simple request factory
        /// </summary>
        /// <typeparam name="TRequest">The type of the request interface to create implementation for.</typeparam>
        /// <typeparam name="TRequestImpl">The type implementing TRequest that has a parameterless constructor.</typeparam>
        public UnversionedRequestFactory RegisterRequest<TRequest, TRequestImpl>()
            where TRequest : IRequest
            where TRequestImpl : TRequest, new()
        {
            if (requestToFactoryMap.ContainsKey(typeof(TRequest)))
            {
                logger.Error($"Registration for {typeof(TRequest).Name} already exists.");
                throw new InvalidOperationException(
                    $"Registration for {typeof(TRequest).Name} already exists.");
            }

            requestToFactoryMap[typeof(TRequest)] = () => new TRequestImpl();
            logger.Debug($"Registered {typeof(TRequestImpl).FullName}");

            return this;
        }

        public TRequest Create<TRequest>(ServerInfo serverInfo) where TRequest : IRequest
        {
            Func<IRequest> factory;
            logger.Debug($"Looking up implementation of '{typeof(TRequest).Name}' on thread '{System.Threading.Thread.CurrentThread.ManagedThreadId}'");
            if (requestToFactoryMap.TryGetValue(typeof(TRequest), out factory))
            {
                    var request = (TRequest)factory();
                    request.Logger = logger;

                    logger.Debug($"Created request of type '{request.GetType().FullName}'.");

                    return request;
            }

            logger.Error($"Could not find factory for '{typeof(TRequest).Name}'.");
            throw new InvalidOperationException($"Could not find factory for '{typeof(TRequest).Name}'.");
        }
    }
}
