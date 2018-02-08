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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal static class VsShellUtils
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S2223:Non-constant static fields should not be visible",
            Justification = "Internal for testing purposes. Cannot make const. Cannot make readonly as is being passed by ref.",
            Scope = "member",
            Target = "~F:SonarLint.VisualStudio.Integration.VsShellUtils.SonarLintOutputPaneGuid")]
        internal static Guid SonarLintOutputPaneGuid = new Guid("EB476B82-D73A-44A6-AFEF-830F7BBA73DB");

        /// <summary>
        /// Writes a message to the SonarLint output pane. Will append a new line after the message.
        /// </summary>
        public static void WriteToSonarLintOutputPane(IServiceProvider serviceProvider, string messageFormat, params object[] args)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (messageFormat == null)
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            IVsOutputWindowPane sonarLintPane = GetOrCreateSonarLintOutputPane(serviceProvider);
            if (sonarLintPane != null)
            {
                WriteLineToPane(sonarLintPane, messageFormat, args);
            }
        }

        /// <summary>
        /// Writes a message to the SonarLint output pane. Will append a new line after the message.
        /// </summary>
        public static void WriteToSonarLintOutputPane(IServiceProvider serviceProvider, string message)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            IVsOutputWindowPane sonarLintPane = GetOrCreateSonarLintOutputPane(serviceProvider);
            if (sonarLintPane != null)
            {
                WriteLineToPane(sonarLintPane, message);
            }
        }

        public static bool IsSolutionExistsAndNotBuildingAndNotDebugging()
        {
            Debug.Assert(KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext != null, "KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebugging is null");
            if (KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext != null)
            {
                return KnownUIContexts.SolutionExistsAndNotBuildingAndNotDebuggingContext.IsActive;
            }
            return false;
        }

        public static bool IsSolutionExistsAndFullyLoaded()
        {
            Debug.Assert(KnownUIContexts.SolutionExistsAndFullyLoadedContext != null, "KnownUIContexts.SolutionExistsAndFullyLoadedContext is null");
            if (KnownUIContexts.SolutionExistsAndFullyLoadedContext != null)
            {
                return KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive;
            }
            return false;
        }

        public static void ActivateSolutionExplorer(IServiceProvider serviceProvider)
        {
            var dte = serviceProvider.GetService<DTE, DTE2>();
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

        public static Property FindProperty(Properties properties, string propertyName)
        {
            return properties?.OfType<Property>()
                .SingleOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.Name, propertyName));
        }

        public static bool SaveSolution(IServiceProvider serviceProvider, bool silent)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var solutionService = serviceProvider.GetService<SVsSolution, IVsSolution>();
            __VSSLNSAVEOPTIONS saveOptions = __VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty;
            if (!silent)
            {
                saveOptions = __VSSLNSAVEOPTIONS.SLNSAVEOPT_PromptSave;
            }

            int hr = solutionService.SaveSolutionElement((uint)saveOptions, null, 0);

            // True if user clicked Yes, false otherwise (No/Cancel/Esc/Close dialog)
            return hr != VSConstants.E_ABORT && ErrorHandler.ThrowOnFailure(hr) == VSConstants.S_OK;
        }


        #region Output pane helpers

        public static IVsOutputWindowPane GetOrCreateSonarLintOutputPane(IServiceProvider serviceProvider)
        {
            IVsOutputWindow outputWindow = serviceProvider.GetService<SVsOutputWindow, IVsOutputWindow>();
            if (outputWindow == null)
            {
                Debug.Fail("Could not get IVsOutputWindow");
                return null;
            }

            const bool makeVisible = true;
            const bool clearWithSolution = false;

            IVsOutputWindowPane pane;

            int hrGetPane = outputWindow.GetPane(ref SonarLintOutputPaneGuid, out pane);
            if (ErrorHandler.Succeeded(hrGetPane))
            {
                return pane;
            }

            int hrCreatePane = outputWindow.CreatePane(
                ref SonarLintOutputPaneGuid,
                Strings.SonarLintOutputPaneTitle,
                Convert.ToInt32(makeVisible),
                Convert.ToInt32(clearWithSolution));
            Debug.Assert(ErrorHandler.Succeeded(hrCreatePane), "Failed in outputWindow.CreatePane: " + hrCreatePane.ToString());

            hrGetPane = outputWindow.GetPane(ref SonarLintOutputPaneGuid, out pane);
            Debug.Assert(ErrorHandler.Succeeded(hrGetPane), "Failed in outputWindow.GetPane: " + hrGetPane.ToString());

            return pane;
        }

        private static void WriteLineToPane(IVsOutputWindowPane pane, string messageFormat, params object[] args)
        {
            int hr = pane.OutputStringThreadSafe(string.Format(CultureInfo.CurrentCulture, messageFormat, args: args) + Environment.NewLine);
            Debug.Assert(ErrorHandler.Succeeded(hr), "Failed in OutputStringThreadSafe: " + hr.ToString());
        }

        private static void WriteLineToPane(IVsOutputWindowPane pane, string message)
        {
            int hr = pane.OutputStringThreadSafe(message + Environment.NewLine);
            Debug.Assert(ErrorHandler.Succeeded(hr), "Failed in OutputStringThreadSafe: " + hr.ToString());
        }

        #endregion
    }
}
