//-----------------------------------------------------------------------
// <copyright file="IProjectSystemHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration
{
    // Test only interface
    internal interface IProjectSystemHelper
    {
        IServiceProvider ServiceProvider { get; }

        Project GetSolutionItemsProject();

        bool IsFileInProject(Project project, string file);

        void AddFileToProject(Project project, string file);

        IEnumerable<Project> GetSolutionManagedProjects();
    }
}
