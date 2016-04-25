//-----------------------------------------------------------------------
// <copyright file="IProjectPropertyManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration
{
    public interface IProjectPropertyManager
    {
        IEnumerable<Project> GetSelectedProjects();

        bool? GetTestProjectProperty(Project project);

        void SetTestProjectProperty(Project project, bool? value);

        bool GetExcludedProperty(Project project);

        void SetExcludedProperty(Project project, bool value);
    }
}