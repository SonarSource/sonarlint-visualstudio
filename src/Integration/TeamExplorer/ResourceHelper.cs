//-----------------------------------------------------------------------
// <copyright file="ResourceHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal static class ResourceHelper
    {
        private static ResourceDictionary sharedResourcesCache;

        public static ResourceDictionary SharedResources
        {
            get
            {
                if (sharedResourcesCache == null)
                {
                    Uri resourceUri = new Uri("/SonarLint.VisualStudio.Integration;component/TeamExplorer/CommonStyles.xaml", UriKind.RelativeOrAbsolute);
                    sharedResourcesCache = (ResourceDictionary)Application.LoadComponent(resourceUri);
                }
                return sharedResourcesCache;
            }
        }

        public static T Get<T>(string resourceName) where T : class
        {
            var resource = SharedResources[resourceName] as T;
            Debug.Assert(resource != null, $"Failed to load resource '{resourceName}' as {typeof(T).Name}");
            return resource;
        }
    }
}
