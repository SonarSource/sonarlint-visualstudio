//-----------------------------------------------------------------------
// <copyright file="IProjectSystemHelperExtensions.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    internal static class IProjectSystemHelperExtensions
    {
        /// <summary>
        /// Returns whether or not a project is of a known test project type.
        /// </summary>
        public static bool IsKnownTestProject(this IProjectSystemHelper projectSystem, IVsHierarchy vsProject)
        {
            if (projectSystem == null)
            {
                throw new ArgumentNullException(nameof(projectSystem));
            }

            if (vsProject == null)
            {
                throw new ArgumentNullException(nameof(vsProject));
            }

            return projectSystem.GetAggregateProjectKinds(vsProject).Contains(ProjectSystemHelper.TestProjectKindGuid);
        }
    }
}
