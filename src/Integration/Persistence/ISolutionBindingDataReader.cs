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
using System.Diagnostics;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal interface ISolutionBindingDataReader
    {
        /// <summary>
        /// Retrieves solution binding information
        /// </summary>
        /// <returns>Can be null if not bound</returns>
        BoundSonarQubeProject Read(string configFilePath);
    }

    internal class SolutionBindingDataReader : ISolutionBindingDataReader
    {
        private readonly ISolutionBindingFileLoader solutionBindingFileLoader;
        private readonly ISolutionBindingCredentialsLoader credentialsLoader;

        public SolutionBindingDataReader(ISolutionBindingFileLoader solutionBindingFileLoader, ISolutionBindingCredentialsLoader credentialsLoader)
        {
            this.solutionBindingFileLoader = solutionBindingFileLoader ?? throw new ArgumentNullException(nameof(solutionBindingFileLoader));
            this.credentialsLoader = credentialsLoader ?? throw new ArgumentNullException(nameof(credentialsLoader));
        }

        public BoundSonarQubeProject Read(string configFilePath)
        {
            var bound = solutionBindingFileLoader.Load(configFilePath);

            if (bound == null)
            {
                return null;
            }

            bound.Credentials = credentialsLoader.Load(bound.ServerUri);

            Debug.Assert(!bound.Profiles?.ContainsKey(Core.Language.Unknown) ?? true,
                "Not expecting the deserialized binding config to contain the profile for an unknown language");

            return bound;
        }
    }
}
