/*
 * SonarLint for Visual Studio
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
using System.Runtime.InteropServices;
using FluentAssertions;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /// <summary>
    /// Configurable service provider used for testing
    /// </summary>
    public class ConfigurableServiceProvider : System.IServiceProvider, Microsoft.VisualStudio.OLE.Interop.IServiceProvider
    {
        private readonly Dictionary<Type, object> serviceInstances = new Dictionary<Type, object>(new TypeComparer());
        private readonly Dictionary<Type, Func<object>> serviceConstructors = new Dictionary<Type, Func<object>>(new TypeComparer());

        // Records the services that were actually requested
        private readonly HashSet<Type> requestedServices = new HashSet<Type>();

        #region Constructor(s)

        public ConfigurableServiceProvider() : this(true)
        {
        }

        public ConfigurableServiceProvider(bool assertOnUnexpectedServiceRequest)
        {
            this.AssertOnUnexpectedServiceRequest = assertOnUnexpectedServiceRequest;
        }

        #endregion Constructor(s)

        #region Test helpers

        /// <summary>
        /// Specifies whether a assertion should be fired if an unregistered service is requested.
        /// If false, requesting an unregistered service will return null.
        /// </summary>
        public bool AssertOnUnexpectedServiceRequest { get; set; }

        /// <summary>
        /// Returns the number of calls to GetService.
        /// </summary>
        public int ServiceCallCount { get; private set; }

        public HashSet<Type> AllRegisteredServices
        {
            get
            {
                return new HashSet<Type>(this.serviceConstructors.Keys.Union(this.serviceInstances.Keys));
            }
        }

        /// <summary>
        /// Registers an instance of a service.
        /// </summary>
        /// <param name="serviceType">Type of the service being registered.</param>
        /// <param name="instance">The instance to return. Can be null.</param>
        /// <remarks>Note: the instance registered can be null (in case we specifically want
        /// to test for a service not being available).</remarks>
        public void RegisterService(Type serviceType, object instance)
        {
            this.RegisterService(serviceType, instance, false);
        }

        public void RegisterService(Type serviceType, object instance, bool replaceExisting)
        {
            serviceType.Should().NotBeNull("Test setup error: serviceType should not be null");

            if (!replaceExisting)
            {
                this.AssertServiceTypeNotRegistered(serviceType);
            }

            this.DoRegisterServiceInstance(serviceType, instance);
        }

        /// <summary>
        /// Registers a service type together with a delegate that is used
        /// to construct it if and when the service is requested.
        /// </summary>
        public void RegisterService(Type serviceType, Func<object> serviceConstructor)
        {
            this.RegisterService(serviceType, serviceConstructor, false);
        }

        public void RegisterService(Type serviceType, Func<object> serviceConstructor, bool replaceExisting)
        {
            serviceType.Should().NotBeNull("Test setup error: serviceType should not be null");
            serviceConstructor.Should().NotBeNull("Test setup error: serviceConstructor should not be null");
            if (!replaceExisting)
            {
                this.AssertServiceTypeNotRegistered(serviceType);
            }

            this.serviceConstructors[serviceType] = serviceConstructor;
        }

        /// <summary>
        /// Resets the tracking information (i.e. the number of calls
        /// made to GetService and the services requested).
        /// Does NOT change the registered services.
        /// </summary>
        public void ResetTracking()
        {
            this.ServiceCallCount = 0;
            this.requestedServices.Clear();
        }

        /// <summary>
        /// Checks that the specified services were used.
        /// </summary>
        public void AssertServicesUsed(params Type[] expectedServiceTypes)
        {
            if (expectedServiceTypes != null)
            {
                foreach (Type t in expectedServiceTypes)
                {
                    this.AssertServiceUsed(t);
                }
            }
        }

        /// <summary>
        /// Checks that the specified service was used.
        /// </summary>
        public void AssertServiceUsed(Type expectedServiceType)
        {
            this.requestedServices.Contains(expectedServiceType).Should().BeTrue("Service Provider: service was not requested: {0}", expectedServiceType.FullName);
        }

        /// <summary>
        /// Checks that the specified service was not used.
        /// </summary>
        public void AssertServiceNotUsed(Type expectedServiceType)
        {
            this.requestedServices.Contains(expectedServiceType).Should().BeFalse("Service Provider: service should not have ben requested: {0}", expectedServiceType.FullName);
        }

        /// <summary>
        /// Checks that all of the registered services were called.
        /// </summary>
        /// <remarks>There are two reasons for this check:
        /// * to do a white-box check that the produce code is using the services we expect; and
        /// * to help keep the tests as simple as possible by highlighting services that are no longer required.</remarks>
        public void AssertAllServicesUsed()
        {
            foreach (Type t in this.AllRegisteredServices)
            {
                this.AssertServicesUsed(t);
            }
        }

        public void AssertExpectedCallCount(int expected)
        {
            this.ServiceCallCount.Should().Be(expected, "GetService was not called the expected number of times");
        }

        #endregion Test helpers

        #region IServiceProvider interface methods

        public object GetService(Type serviceType)
        {
            serviceType.Should().NotBeNull("serviceType should not be null");
            this.requestedServices.Add(serviceType);
            this.ServiceCallCount++;

            // Try to get an existing service instance (which could be null)
            object serviceInstance = null;
            bool found = this.serviceInstances.TryGetValue(serviceType, out serviceInstance);

            // If we didn't find an instance, try to create one.
            if (!found)
            {
                Func<object> constructor = null;
                found = this.serviceConstructors.TryGetValue(serviceType, out constructor);
                if (found)
                {
                    serviceInstance = constructor();
                    // Store the created instance in case we need it again.
                    this.DoRegisterServiceInstance(serviceType, serviceInstance);
                }
            }

            if (!found && this.AssertOnUnexpectedServiceRequest)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Unexpected GetService for type: " + serviceType.FullName);
            }

            return serviceInstance;
        }

        #endregion IServiceProvider interface methods

        #region OLE IServiceProvider

        int Microsoft.VisualStudio.OLE.Interop.IServiceProvider.QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
        {
            object service = this.GetService(this.FindTypeForGuid(guidService));
            if (service != null)
            {
                IntPtr serviceInterface = Marshal.GetIUnknownForObject(service);
                if (serviceInterface != IntPtr.Zero)
                {
                    return Marshal.QueryInterface(serviceInterface, ref riid, out ppvObject);
                }
            }

            ppvObject = IntPtr.Zero;
            return Microsoft.VisualStudio.VSConstants.E_FAIL;
        }

        #endregion OLE IServiceProvider

        #region Private methods

        private Type FindTypeForGuid(Guid typeGuid)
        {
            return this.serviceInstances.Keys.Concat(this.serviceConstructors.Keys).First(t => t.GUID == typeGuid);
        }

        private void AssertServiceTypeNotRegistered(Type serviceType)
        {
            this.AllRegisteredServices.Contains(serviceType).Should().BeFalse("Test setup error: a service instance or constructor for this type has already been registered: {0}",
                serviceType.FullName);
        }

        private void DoRegisterServiceInstance(Type serviceType, object serviceInstance)
        {
            this.serviceInstances[serviceType] = serviceInstance;
        }

        #endregion Private methods

        #region Helpers

        private class TypeComparer : IEqualityComparer<Type>
        {
            // Service providers must use type-equivalent type comparisons to support embedded interop types
            public bool Equals(Type t1, Type t2)
            {
                return t1.IsEquivalentTo(t2);
            }

            public int GetHashCode(Type t)
            {
                return t.FullName.GetHashCode();
            }
        }

        #endregion Helpers
    }
}