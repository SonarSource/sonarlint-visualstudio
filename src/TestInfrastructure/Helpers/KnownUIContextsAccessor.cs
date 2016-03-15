//-----------------------------------------------------------------------
// <copyright file="KnownUIContextsAccessor.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

            Assert.IsTrue(KnownUIContextsProperties.All(pi => pi.GetValue(null) != null), "UIContext failed to register");
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
