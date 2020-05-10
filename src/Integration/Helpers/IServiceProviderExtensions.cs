/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using SonarLint.VisualStudio.Core;

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
            return GetMefService<T>(componentModel);
        }

        public static System.Threading.Tasks.Task<T> GetMefServiceAsync<T>(this Microsoft.VisualStudio.Shell.IAsyncServiceProvider serviceProvider)
            where T : class
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            return GetMefServiceIteratorAsync<T>(serviceProvider);
        }

        private static async System.Threading.Tasks.Task<T> GetMefServiceIteratorAsync<T>(this Microsoft.VisualStudio.Shell.IAsyncServiceProvider serviceProvider)
            where T : class
        {
            IComponentModel componentModel = await serviceProvider.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            return GetMefService<T>(componentModel);
        }

        public static T GetMefService<T>(this IComponentModel componentModel)
            where T : class
        {
            // We don't want to throw in the case of a missing service (don't use GetService<T>)
            var results = componentModel?.GetExtensions<T>().ToList();

            if (results == null)
            {
                return null;
            }
            else if (results.Count == 0)
            {
                Debug.Fail($"Cannot find any export of {typeof(T)}");
                return null;
            }
            else if (results.Count > 1)
            {
                Debug.Fail($"Not expecting to find multiple export of {typeof(T)}");
                return null;
            }
            else
            {
                return results[0];
            }
        }

        [Conditional("DEBUG")]
        // Note: Conditional DEBUG methods are still compiled in release mode but won't be called.
        // However, they do contribute to the lines of code for code coverage purposes, so if
        // possible use #if DEBUG instead.
        internal static void AssertLocalServiceIsNotNull<T>(this T service)
            where T : class, ILocalService
        {
            Debug.Assert(service != null, $"Local service {typeof(T).FullName} is not registered");
        }
    }
}
