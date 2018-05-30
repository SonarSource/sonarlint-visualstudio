using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarQube.Client
{
    public class RequestFactory
    {
        private readonly Dictionary<Type, SortedList<Version, Func<object>>> requestMappings =
            new Dictionary<Type, SortedList<Version, Func<object>>>();

        public RequestFactory RegisterRequest<TRequest, TRequestImpl>(string version)
            where TRequest : IRequest
            where TRequestImpl : TRequest, new()
        {
            return RegisterRequest<TRequest, TRequestImpl>(version, () => new TRequestImpl());
        }

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
                    .LastOrDefault(entry => version == null || entry.Key < version)
                    .Value;

                if (factory != null)
                {
                    return (TRequest)factory();
                }

                throw new InvalidOperationException($"Could not find compatible implementation of '{typeof(TRequest).Name}' for SonarQube {version}.");
            }
            throw new InvalidOperationException($"Could not find implementation for '{typeof(TRequest).Name}'.");
        }
    }
}
