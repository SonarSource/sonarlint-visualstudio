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

using System;
using System.Collections.Generic;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.TypeScript.NodeJSLocator.LocationProviders
{
    internal class EnvironmentVariableNodeLocationsProvider : INodeLocationsProvider
    {
        internal const string NodeJsPathEnvVar = "SONAR_NODEJS_PATH";

        private readonly ILogger logger;

        public EnvironmentVariableNodeLocationsProvider(ILogger logger)
        {
            this.logger = logger;
        }

        public IReadOnlyCollection<string> Get()
        {
            logger.WriteLine(Resources.INFO_CheckingEnvVar, NodeJsPathEnvVar);
            var nodeExePath = Environment.GetEnvironmentVariable(NodeJsPathEnvVar);

            if (string.IsNullOrEmpty(nodeExePath))
            {
                logger.WriteLine(Resources.INFO_NoEnvVar);
                return Array.Empty<string>();
            }

            logger.WriteLine(Resources.INFO_EnvVarValue, nodeExePath);
            return new[] {nodeExePath};
        }
    }
}
