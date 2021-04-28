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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    interface IEslintBridgeClientFactory
    {
        IEslintBridgeClient Create();
    }

    [Export(typeof(IEslintBridgeClientFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]

    internal sealed class EslintBridgeClientFactory : IEslintBridgeClientFactory
    {
        private readonly IEslintBridgeProcessFactory eslintBridgeProcessFactory;
        private readonly ILogger logger;

        [ImportingConstructor]
        public EslintBridgeClientFactory(IEslintBridgeProcessFactory eslintBridgeProcessFactory, ILogger logger)
        {
            this.eslintBridgeProcessFactory = eslintBridgeProcessFactory;
            this.logger = logger;
        }

        public IEslintBridgeClient Create() => new EslintBridgeClient(eslintBridgeProcessFactory, logger);
    }
}
