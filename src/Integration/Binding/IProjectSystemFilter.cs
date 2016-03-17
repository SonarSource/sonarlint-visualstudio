//-----------------------------------------------------------------------
// <copyright file="IProjectSystemFilter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace SonarLint.VisualStudio.Integration.Binding
{
    internal interface IProjectSystemFilter
    {
        /// <summary>
        /// Returns true if the given project passed the filter criteria. False otherwise.
        /// </summary>
        bool IsAccepted(EnvDTE.Project project);

        /// <summary>
        /// Set regular expression to be used to identify a test project.
        /// </summary>
        void SetTestRegex(Regex regex);
    }
}