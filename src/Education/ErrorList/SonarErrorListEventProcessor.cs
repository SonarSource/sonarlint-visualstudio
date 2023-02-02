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

using Microsoft;
using Microsoft.VisualStudio.Shell.TableControl;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.Education
{
    namespace SonarLint.VisualStudio.Education.ErrorList
    {
        internal class SonarErrorListEventProcessor : TableControlEventProcessorBase
        {
            private readonly IEducation educationService;
            private readonly IErrorListHelper errorListHelper;
            private readonly ILogger logger;


            public SonarErrorListEventProcessor(IEducation educationService, IErrorListHelper errorListHelper, ILogger logger)
            {
                this.educationService = educationService;
                this.errorListHelper = errorListHelper;
                this.logger = logger;
            }

            public override void PreprocessNavigateToHelp(
               ITableEntryHandle entryHandle,
               TableEntryEventArgs e)
            {
                Requires.NotNull(entryHandle, nameof(entryHandle));

                bool handled = false;

                if (errorListHelper.TryGetRuleId(entryHandle, out var ruleId))
                {
                    logger.LogVerbose(Resources.ErrorList_Processor_SonarRuleDetected, ruleId);

                    var language = Language.GetLanguageFromRepositoryKey(ruleId.RepoKey);
                    educationService.ShowRuleDescription(language, ruleId.RuleKey);
                    handled = true;
                }

                e.Handled = handled;
            }
        }
    }
}
