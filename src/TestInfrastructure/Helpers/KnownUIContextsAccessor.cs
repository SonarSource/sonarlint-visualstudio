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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public static class KnownUIContextsAccessor
    {
        // KnownUIContexts is not very friendly for testing, and requires this static properties
        static KnownUIContextsAccessor()
        {
            ServiceProvider = VsServiceProviderHelper.GlobalServiceProvider;
            MonitorSelectionService = new ConfigurableVsMonitorSelection();
            ServiceProvider.RegisterService(typeof(IVsMonitorSelection), MonitorSelectionService, true);
            Reset();
        }

        public static ConfigurableVsMonitorSelection MonitorSelectionService
        {
            get;
            private set;
        }

        public static ConfigurableServiceProvider ServiceProvider
        {
            get;
        }

        public static void Reset()
        {
            MonitorSelectionService.UIContexts
                .ToList()
                .ForEach(contextId => MonitorSelectionService.SetContext(contextId, false));

            KnownUIContextsProperties.All(pi => pi.GetValue(null) != null).Should().BeTrue("UIContext failed to register");
        }

        private static IEnumerable<PropertyInfo> KnownUIContextsProperties
        {
            get
            {
                return typeof(KnownUIContexts)
                    .GetProperties(BindingFlags.Static | BindingFlags.Public)
                    .Where(p => p.PropertyType.IsEquivalentTo(typeof(UIContext)));
            }
        }
    }
}