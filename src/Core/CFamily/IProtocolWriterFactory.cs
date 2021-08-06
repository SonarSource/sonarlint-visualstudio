/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

namespace SonarLint.VisualStudio.Core.CFamily
{
    public interface IProtocolWriterFactory
    {
        /// <summary>
        /// Creates a <see cref="IProtocolWriter"/> that can write the given <see cref="IRequest"/>
        /// in one of the formats supported by the subprocess.
        /// Will return null if an appropriate protocol cannot be determined.
        /// </summary>
        /// <remarks>
        /// All of the subprocess protocols require additional data that is not in the <see cref="IRequest"/>,
        /// so a writer will only be created if that additional data can be located.
        /// </remarks>
        IProtocolWriter TryGet(IRequest request);
    }
}
