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

using System;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    [Export(typeof(IAnalysisStatusNotifierFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class AnalysisStatusNotifierFactory : IAnalysisStatusNotifierFactory
    {
        private readonly IStatusBarNotifier statusBarNotifier;
        private readonly ILogger logger;

        [ImportingConstructor]
        public AnalysisStatusNotifierFactory(IStatusBarNotifier statusBarNotifier, ILogger logger)
        {
            this.statusBarNotifier = statusBarNotifier;
            this.logger = logger;
        }

        public IAnalysisStatusNotifier Create(string analyzerName, string filePath)
        {
            if (analyzerName == null)
            {
                throw new ArgumentNullException(nameof(analyzerName));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            return new AnalysisStatusNotifier(analyzerName, filePath, statusBarNotifier, logger);
        }
    }
}
