/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Threading.Tasks;

namespace SonarLint.VisualStudio.CFamily.Analysis
{
    public interface IRequestFactory
    {
        /// <summary>
        /// Creates <see cref="IRequest"/> for the given <see cref="analyzedFilePath"/>.
        /// Returns null if request could not be created.
        /// </summary>
        Task<IRequest> TryCreateAsync(string analyzedFilePath, CFamilyAnalyzerOptions analyzerOptions);
    }

    /// <summary>
    /// Aggregate interface for multiple <see cref="IRequestFactory"/>.
    /// <see cref="IRequestFactory.TryCreateAsync"/> will return the first non-nullable request,
    /// or null if no factory was able to create one.
    /// </summary>
    internal interface IRequestFactoryAggregate : IRequestFactory
    {}
}
