//-----------------------------------------------------------------------
// <copyright file="IServiceProviderExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of service object</typeparam>
        public static T GetService<T>(this IServiceProvider serviceProvider)
            where T : class
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            return serviceProvider.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Gets the service object of the specified type and safe-cast to
        /// the specified return type.
        /// </summary>
        /// <typeparam name="T">Type of service object</typeparam>
        /// <typeparam name="U">Cast return type</typeparam>
        public static U GetService<T, U>(this IServiceProvider serviceProvider)
            where T : class
            where U : class
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            return serviceProvider.GetService(typeof(T)) as U;
        }

        public static T GetMefService<T>(this IServiceProvider serviceProvider)
            where T : class
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            IComponentModel componentModel = serviceProvider.GetService<SComponentModel, IComponentModel>();
            // We don't want to throw in the case of a missing service (don't use GetService<T>)  
            return componentModel?.GetExtensions<T>().SingleOrDefault();
        }

        [Conditional("DEBUG")]
        internal static void AssertLocalServiceIsNotNull<T>(this T service)
            where T : class, ILocalService
        {
            Debug.Assert(service != null, $"Local service {typeof(T).FullName} is not registered");
        }
    }
}
