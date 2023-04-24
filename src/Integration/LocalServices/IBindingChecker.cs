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

using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal interface IBindingChecker
    {
        /// <summary>
        /// Returns true/false if the currently opened solution/folder and all the projects in it are properly bound
        /// </summary>
        Task<bool> IsBindingUpdateRequired(CancellationToken token);
    }

    internal class BindingChecker : IBindingChecker
    {
        private readonly IUnboundSolutionChecker unboundSolutionChecker;
        private readonly ILogger logger;

        public BindingChecker(IUnboundSolutionChecker unboundSolutionChecker, 
            ILogger logger)
        {
            this.unboundSolutionChecker = unboundSolutionChecker;
            this.logger = logger;
        }

        public async Task<bool> IsBindingUpdateRequired(CancellationToken token)
        {
            var solutionBindingRequired = await unboundSolutionChecker.IsBindingUpdateRequired(token);

            if (solutionBindingRequired)
            {
                logger.WriteLine(Strings.SonarLintFoundUnboundSolution);
                return true;
            }

            return false;
        }
    }
}
