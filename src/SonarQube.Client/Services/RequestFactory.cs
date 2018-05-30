/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Linq;

namespace SonarQube.Client
{
    public class RequestFactory
    {
        private readonly Dictionary<Type, SortedList<Version, Func<object>>> requestMappings =
            new Dictionary<Type, SortedList<Version, Func<object>>>();

        /// <summary>
        /// Registers a simple request factory for the specified version of SonarQube. It works only for types that
        /// have parameterless constructor.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request interface to create implementation for.</typeparam>
        /// <typeparam name="TRequestImpl">The type implementing TRequest that has a parameterless constructor.</typeparam>
        /// <param name="version">The version of SonarQube which first implements the request.</param>
        /// <returns>Returns this.</returns>
        public RequestFactory RegisterRequest<TRequest, TRequestImpl>(string version)
            where TRequest : IRequest
            where TRequestImpl : TRequest, new()
        {
            return RegisterRequest<TRequest, TRequestImpl>(version, () => new TRequestImpl());
        }

        /// <summary>
        /// Registers a request factory for the specified version of SonarQube.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request interface to create implementation for.</typeparam>
        /// <typeparam name="TRequestImpl">The type implementing TRequest.</typeparam>
        /// <param name="version">The version of SonarQube which first implements the request.</param>
        /// <param name="factory">Factory function to create new instances of TRequestImpl.</param>
        /// <returns>Returns this.</returns>
        public RequestFactory RegisterRequest<TRequest, TRequestImpl>(string version, Func<TRequestImpl> factory)
            where TRequest : IRequest
            where TRequestImpl : TRequest
        {
            SortedList<Version, Func<object>> map;
            if (!requestMappings.TryGetValue(typeof(TRequest), out map))
            {
                map = new SortedList<Version, Func<object>>();
                requestMappings[typeof(TRequest)] = map;
            }
            map[Version.Parse(version)] = () => factory();
            return this;
        }

        /// <summary>
        /// Creates a new TRequest implementation for the specified SonarQube version.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request implementation to create.</typeparam>
        /// <param name="version">
        /// SonarQube version to return a request implementation for. The default value returns the
        /// latest registered implementation.QueryStringSerializer
        /// </param>
        /// <returns>New TRequest implementation for the specified SonarQube version.</returns>
        public TRequest Create<TRequest>(Version version = null)
            where TRequest : IRequest
        {
            SortedList<Version, Func<object>> map;
            if (requestMappings.TryGetValue(typeof(TRequest), out map))
            {
                var factory = map
                    .LastOrDefault(entry => version == null || entry.Key <= version)
                    .Value;

                if (factory != null)
                {
                    return (TRequest)factory();
                }

                throw new InvalidOperationException($"Could not find compatible implementation of '{typeof(TRequest).Name}' for SonarQube {version}.");
            }
            throw new InvalidOperationException($"Could not find factory for '{typeof(TRequest).Name}'.");
        }
    }
}
