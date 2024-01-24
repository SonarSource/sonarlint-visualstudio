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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.JsTs;

namespace SonarLint.VisualStudio.TypeScript.EslintBridgeClient
{
    internal interface IEslintBridgeProcessFactory
    {
        IEslintBridgeProcess Create();
    }

    [Export(typeof(IEslintBridgeProcessFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class EslintBridgeProcessFactory : IEslintBridgeProcessFactory
    {
        internal const string EslintBridgeDirectoryMefContractName = "SonarLint.TypeScript.EsLintBridgeServerPath";

        private readonly string eslintBridgeStartupScriptPath;
        private readonly ICompatibleNodeLocator compatibleNodeLocator;
        private readonly ILogger logger;

        [ImportingConstructor]
        public EslintBridgeProcessFactory([Import(EslintBridgeDirectoryMefContractName)] string eslintBridgeStartupScriptPath,
            ICompatibleNodeLocator compatibleNodeLocator,
            ILogger logger)
        {
            this.eslintBridgeStartupScriptPath = eslintBridgeStartupScriptPath;
            this.compatibleNodeLocator = compatibleNodeLocator;
            this.logger = logger;
        }

        public IEslintBridgeProcess Create() => new EslintBridgeProcess(eslintBridgeStartupScriptPath, compatibleNodeLocator, logger);
    }
}
