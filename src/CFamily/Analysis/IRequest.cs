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

using System.Collections.Generic;
using System.IO;

namespace SonarLint.VisualStudio.CFamily.Analysis
{
    /// <summary>
    /// Encapsulates a low-level request to be sent to the CFamily subprocess
    /// </summary>
    /// <remarks>The subprocess supports several different protocols, each of which requires a different set of inputs.</remarks>
    public interface IRequest
    {
        RequestContext Context { get; }

        /// <summary>
        /// Serializes the request in the form required by the subprocess
        /// </summary>
        void WriteRequest(BinaryWriter writer);

        /// <summary>
        /// Any environment variables that need to be passed to the subprocess. Can be null.
        /// </summary>
        IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

        /// <summary>
        /// Serializes the request for diagnostic purposes
        /// </summary>
        void WriteRequestDiagnostics(TextWriter writer);
    }
}
