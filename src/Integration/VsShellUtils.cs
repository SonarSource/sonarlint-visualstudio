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
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    public static class VsShellUtils
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
        /// Writes a message to the general output pane. Will append a new line after the message.
        /// </summary>
        public static void WriteToGeneralOutputPane(IServiceProvider serviceProvider, string messageFormat, params object[] args)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (messageFormat == null)
            {
                throw new ArgumentNullException(nameof(messageFormat));
            }

            IVsOutputWindowPane generalPane = serviceProvider.GetService(typeof(SVsGeneralOutputWindowPane)) as IVsOutputWindowPane;
            if (generalPane != null)
            {
                WriteLineToPane(generalPane, messageFormat, args);
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

            var solutionService = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            __VSSLNSAVEOPTIONS saveOptions = __VSSLNSAVEOPTIONS.SLNSAVEOPT_SaveIfDirty;
            if (!silent)
            {
                saveOptions |= __VSSLNSAVEOPTIONS.SLNSAVEOPT_PromptSave;
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

            // Try and get pane if it already exists
            IVsOutputWindowPane pane;
            if (ErrorHandler.Failed(outputWindow.GetPane(ref SonarLintOutputPaneGuid, out pane)))
            {
                // Create new pane
                const bool makeVisible = true;
                const bool clearWithSolution = true;

                int hrCreatePane = outputWindow.CreatePane(
                ref SonarLintOutputPaneGuid,
                Strings.SonarLintOutputPaneTitle,
                Convert.ToInt32(makeVisible),
                Convert.ToInt32(clearWithSolution));
                Debug.Assert(ErrorHandler.Succeeded(hrCreatePane), "Failed in outputWindow.CreatePane: " + hrCreatePane.ToString());
                
                // Get newly created pane
                int hrGetPane = outputWindow.GetPane(ref SonarLintOutputPaneGuid, out pane);
                Debug.Assert(ErrorHandler.Succeeded(hrGetPane), "Failed in outputWindow.GetPane: " + hrGetPane.ToString());
            }
            
            return pane;
        }

        private static void WriteLineToPane(IVsOutputWindowPane pane, string messageFormat, params object[] args)
        {
            int hr = pane.OutputStringThreadSafe(Environment.NewLine + string.Format(CultureInfo.CurrentCulture, messageFormat, args: args));
            Debug.Assert(ErrorHandler.Succeeded(hr), "Failed in OutputStringThreadSafe: " + hr.ToString());
        }

        #endregion
    }
}
