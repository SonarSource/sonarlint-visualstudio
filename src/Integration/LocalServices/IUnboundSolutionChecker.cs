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

using System;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal interface IUnboundSolutionChecker
    {
        /// <summary>
        /// Returns true/false if the currently opened solution/folder is bound and requires re-binding
        /// </summary>
        bool IsBindingUpdateRequired();
    }

    internal class UnboundSolutionChecker : IUnboundSolutionChecker
    {
        private readonly IExclusionSettingsStorage exclusionSettingsStorage;
        private readonly ILogger logger;

        public UnboundSolutionChecker(IExclusionSettingsStorage exclusionSettingsStorage, ILogger logger)
        {
            this.exclusionSettingsStorage = exclusionSettingsStorage;
            this.logger = logger;
        }

        public bool IsBindingUpdateRequired()
        {
            try
            {
                return !exclusionSettingsStorage.SettingsExist();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.LogDebug("[UnboundSolutionChecker] Failed to check for settings: {0}", ex.ToString());
                logger.WriteLine(Strings.BindingUpdateFailedToCheckSettings, ex.Message);

                return false;
            }
        }
    }
}
