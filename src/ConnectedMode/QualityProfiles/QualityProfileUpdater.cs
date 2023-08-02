/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.QualityProfiles
{
    internal interface IQualityProfileUpdater
    {
        /// <summary>
        /// When in Connected Mode, ensures that all of the Quality Profiles
        /// are up to date
        /// </summary>
        Task UpdateAsync(CancellationToken cancellationToken);
    }

    [Export(typeof(IQualityProfileUpdater))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class QualityProfileUpdater : IQualityProfileUpdater
    {
        public Task UpdateAsync(CancellationToken cancellationToken)
        {
            // TODO - implement
            return Task.CompletedTask;
        }
    }
}
