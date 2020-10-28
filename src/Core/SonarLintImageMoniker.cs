/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Duplication of Microsoft.VisualStudio.Imaging.Interop.ImageMoniker struct, used to avoid adding a reference to VS assembly.
    /// </summary>
    public readonly struct SonarLintImageMoniker
    {
        public SonarLintImageMoniker(Guid guid, int id)
        {
            Guid = guid;
            Id = id;
        }

        public Guid Guid { get; }
        public int Id { get; }
    }
}
