/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.QualityProfiles
{
    internal interface IQualityProfileDownloader
    {
        /// <summary>
        /// Ensures that the Quality Profiles for all supported languages are to date
        /// </summary>
        /// <returns>true if there were changes updated, false if everything is up to date</returns>
        /// <exception cref="InvalidOperationException">If binding failed for one of the languages</exception>
        Task<bool> UpdateAsync(BoundServerProject boundProject, IProgress<FixedStepsProgress> progress, CancellationToken cancellationToken);
    }

    [Export(typeof(IQualityProfileDownloader))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    [ExcludeFromCodeCoverage] // todo https://sonarsource.atlassian.net/browse/SLVS-2420
    internal class RoslynQualityProfileDownloader()
        : IQualityProfileDownloader
    {
        public async Task<bool> UpdateAsync(
            BoundServerProject boundProject,
            IProgress<FixedStepsProgress> progress,
            CancellationToken cancellationToken)
        {
            // TODO by https://sonarsource.atlassian.net/browse/SLVS-2420 drop this class
            return false;
        }
    }
}
