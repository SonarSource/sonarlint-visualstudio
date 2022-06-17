/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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

using System.Linq;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal interface IBindingChecker
    {
        /// <summary>
        /// Returns true/false if the currently opened solution/folder is bound and requires re-binding
        /// </summary>
        bool IsBindingUpdateRequired();
    }

    internal class BindingChecker : IBindingChecker
    {
        private readonly IUnboundSolutionChecker unboundSolutionChecker;
        private readonly IUnboundProjectFinder unboundProjectFinder;
        private readonly ILogger logger;

        public BindingChecker(IUnboundSolutionChecker unboundSolutionChecker, 
            IUnboundProjectFinder unboundProjectFinder, 
            ILogger logger)
        {
            this.unboundSolutionChecker = unboundSolutionChecker;
            this.unboundProjectFinder = unboundProjectFinder;
            this.logger = logger;
        }

        public bool IsBindingUpdateRequired()
        {
            if (unboundSolutionChecker.IsBindingUpdateRequired())
            {
                logger.WriteLine(Strings.SonarLintFoundUnboundSolution);
                return true;
            }

            var unboundProjects = unboundProjectFinder.GetUnboundProjects().ToArray();
            var hasUnboundProjects = unboundProjects.Length > 0;

            if (hasUnboundProjects)
            {
                logger.WriteLine(Strings.SonarLintFoundUnboundProjects, unboundProjects.Length, string.Join(", ", unboundProjects.Select(p => p.UniqueName)));
            }

            return hasUnboundProjects;
        }
    }
}
