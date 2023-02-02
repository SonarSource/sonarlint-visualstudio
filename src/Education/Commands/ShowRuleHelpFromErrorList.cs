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
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Education.Commands
{
    public interface IShowHelpFromErrorList
    { }

    [Export(typeof(IShowHelpFromErrorList))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ShowHelpFromErrorList : IShowHelpFromErrorList
    {
        private readonly IThreadHandling threadHandling;
        private readonly IEducation education;
        private readonly IErrorListHelper errorListHelper;

        private readonly Guid showHelpCmdGuid = new Guid("4A9B7E50-AA16-11D0-A8C5-00A0C921A4D2");
        private const int showHelpCmdId = 598;

        [ImportingConstructor]
        public ShowHelpFromErrorList([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IEducation education, IErrorListHelper errorListHelper)
            : this(serviceProvider, education, errorListHelper, ThreadHandling.Instance)
        {
        }

        internal ShowHelpFromErrorList(IServiceProvider serviceProvider, IEducation education, IErrorListHelper errorListHelper, IThreadHandling threadHandling)
        {
            this.education = education;
            this.errorListHelper = errorListHelper;
            this.threadHandling = threadHandling;

            InitializeInterception(serviceProvider);
        }

        internal void InitializeInterception(IServiceProvider serviceProvider)
        {
            threadHandling.ThrowIfNotOnUIThread();

            var priority = (IVsRegisterPriorityCommandTarget)serviceProvider.GetService(typeof(SVsRegisterPriorityCommandTarget));

            var command = new CommandID(showHelpCmdGuid, showHelpCmdId);

            var interceptor = new CommandInterceptor(command, () => HandleInterception());

            priority.RegisterPriorityCommandTarget(0, interceptor, out uint cookie);
        }

        /// <summary>
        /// If the currently selected error code is a Sonar code show the rule description and
        /// stop progression to next command else move on.
        /// </summary>
        /// <returns></returns>
        internal CommandProgression HandleInterception()
        {
            if (errorListHelper.TryGetRuleIdFromSelectedRow(out SonarCompositeRuleId ruleId))
            {
                var language = Language.GetLanguageFromRepositoryKey(ruleId.RepoKey);
                education.ShowRuleDescription(language, ruleId.RuleKey);
                return CommandProgression.Stop;
            }

            return CommandProgression.Continue;
        }
    }
}
