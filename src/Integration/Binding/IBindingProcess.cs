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
using System.Threading;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Encapsulates the set of operations carried out in sequence by the binding workflow.
    /// The operations are expected to be called in the order in which they are defined in this file.
    /// </summary>
    /// <remarks>This interface as extracted from the <see cref="BindingWorkflow"/> class to reduce
    /// the complexity of that class and simplify testing. The <see cref="BindingWorkflow"/> class is responsible
    /// for handling threading and progress reporting in the UI. This class is responsible for the
    /// making the changes to projects and files.</remarks>
    internal interface IBindingProcess
    {
        bool PromptSaveSolutionIfDirty();

        bool DiscoverProjects();

        System.Threading.Tasks.Task<bool> DownloadQualityProfileAsync(IProgress<FixedStepsProgress> progress, IEnumerable<Language> languages, CancellationToken cancellationToken);

        // duncanp - remove
        IEnumerable<Language> GetBindingLanguages();

        void PrepareToInstallPackages();

        void InstallPackages(IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken);

        void InitializeSolutionBindingOnUIThread();

        void PrepareSolutionBinding(CancellationToken cancellationToken);

        bool FinishSolutionBindingOnUIThread();

        void SilentSaveSolutionIfDirty();

        bool BindOperationSucceeded { get; }
    }
}
