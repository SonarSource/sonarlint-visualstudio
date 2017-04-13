/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
