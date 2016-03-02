//-----------------------------------------------------------------------
// <copyright file="VsShellUtils.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    public static class VsShellUtils
    {
        /// <summary>
        /// Writes a message to the general output pane. Will append a new line after the message.
        /// </summary>
        public static void WriteToGeneralOutputPane(IServiceProvider serviceProvider, String messageFormat, params object[] args)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (messageFormat == null)
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            IVsOutputWindowPane generalPanel = serviceProvider.GetService(typeof(SVsGeneralOutputWindowPane)) as IVsOutputWindowPane;
            if (generalPanel != null)
            {
                int hr = generalPanel.OutputStringThreadSafe(Environment.NewLine + string.Format(CultureInfo.CurrentCulture, messageFormat, args: args));
                Debug.Assert(ErrorHandler.Succeeded(hr), "Failed in OutputStringThreadSafe: " + hr.ToString());
            }
        }

        public static bool IsSolutionExistsAndNotBuildingAndNotDebugging(Action onChange = null)
        {
            Debug.Assert(KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext != null, "KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebugging is null");
            if (KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext != null)
            {
                return KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;
            }
            return false;
        }

        public static void ActivateSolutionExplorer(IServiceProvider serviceProvider)
        {
            var dte = serviceProvider.GetService(typeof(DTE)) as DTE2;
            dte.ToolWindows.SolutionExplorer.Parent.Activate();
        }

        public static IEnumerable<Property> EnumerateProjectProperties(Project project, string propertyName)
        {
            // Try to find the property on the project level properties (relevant for legacy web projects only)
            Property property = FindProperty(project.Properties, propertyName);
            if (property == null)
            {
                // Try to find the property for specific configuration (managed and web 4.5 onward)
                Configuration[] configurations = project.ConfigurationManager.OfType<Configuration>().ToArray();
                if (configurations.Length == 0)
                {
                    yield break;
                }

                foreach (var configuration in configurations)
                {
                    yield return FindProperty(configuration.Properties, propertyName);
                }
            }
            else
            {
                yield return property;
            }
        }

        private static Property FindProperty(Properties properties, string propertyName)
        {
            return properties?.OfType<Property>()
                .Where(p => StringComparer.OrdinalIgnoreCase.Equals(p.Name, propertyName))
                .SingleOrDefault();
        }

        internal static IEnumerable<Property> EnumerateProjectProperties(Project project, object additionalFilePropertyKey)
        {
            throw new NotImplementedException();
        }
    }
}
