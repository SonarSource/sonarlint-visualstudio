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
        /// <summary>
        /// Map between request type and a list with versioned request implementation factories.
        /// Each request type could have implementations for multiple versions of SonarQube. The
        /// SortedList is a map between the minimum supported version of the request implementation
        /// and the corresponding factory.
        /// </summary>
        private readonly Dictionary<Type, SortedList<Version, Func<IRequest>>> registrations =
            new Dictionary<Type, SortedList<Version, Func<IRequest>>>();

        private Action<string> Log { get; }

        public RequestFactory(Action<string> log = null)
        {
            Log = log ?? (s => { });
        }

        /// <summary>
        /// Registers a simple request factory for the specified version of SonarQube.
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

        private RequestFactory RegisterRequest<TRequest, TRequestImpl>(string version, Func<TRequestImpl> factory)
            where TRequest : IRequest
            where TRequestImpl : TRequest
        {
            Version parsedVersion;
            if (!Version.TryParse(version, out parsedVersion))
            {
                throw new ArgumentException($"Invalid version string '{version}'.", nameof(version));
            }

            SortedList<Version, Func<IRequest>> versionRequestMap;
            if (!registrations.TryGetValue(typeof(TRequest), out versionRequestMap))
            {
                versionRequestMap = new SortedList<Version, Func<IRequest>>();
                registrations[typeof(TRequest)] = versionRequestMap;
            }
            else if (versionRequestMap.ContainsKey(parsedVersion))
            {
                throw new InvalidOperationException(
                    $"Registration for {typeof(TRequest).Name} with version {version} already exists.");
            }

            versionRequestMap[parsedVersion] = () => factory();

            return this;
        }

        /// <summary>
        /// Creates a new TRequest implementation for the specified SonarQube version.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request implementation to create.</typeparam>
        /// <param name="version">
        /// SonarQube version to return a request implementation for. When the provided value is null
        /// returns the registered implementation with the highest version number.
        /// </param>
        /// <returns>New the newest TRequest implementation for the specified SonarQube version.</returns>
        public TRequest Create<TRequest>(Version version)
            where TRequest : IRequest
        {
            SortedList<Version, Func<IRequest>> map;
            if (registrations.TryGetValue(typeof(TRequest), out map))
            {
                Log($"Looking up implementation of '{typeof(TRequest).Name}' for version '{version}' on thread '{System.Threading.Thread.CurrentThread.ManagedThreadId}'");

                var factory = map
                    .LastOrDefault(entry => version == null || entry.Key <= version)
                    .Value;

                if (factory != null)
                {
                    var request = (TRequest)factory();

                    Log($"Created request of type '{request.GetType().FullName}'.");

                    return request;
                }

                throw new InvalidOperationException($"Could not find compatible implementation of '{typeof(TRequest).Name}' for SonarQube {version}.");
            }

            throw new InvalidOperationException($"Could not find factory for '{typeof(TRequest).Name}'.");
        }
    }
}
