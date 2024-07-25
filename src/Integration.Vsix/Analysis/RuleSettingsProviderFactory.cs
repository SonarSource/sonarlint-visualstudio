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
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.UserRuleSettings;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis
{
    [Export(typeof(IRuleSettingsProviderFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class RuleSettingsProviderFactory : IRuleSettingsProviderFactory
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IUserSettingsProvider userSettingsProvider;
        private readonly IRulesSettingsSerializer serializer;
        private readonly ILogger logger;

        [ImportingConstructor]
        public RuleSettingsProviderFactory(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IUserSettingsProvider userSettingsProvider,
            ILogger logger)
            : this(activeSolutionBoundTracker,
                userSettingsProvider,
                new RulesSettingsSerializer(new FileSystem(), logger),
                logger)
        {
        }

        internal RuleSettingsProviderFactory(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IUserSettingsProvider userSettingsProvider,
            IRulesSettingsSerializer serializer,
            ILogger logger)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.userSettingsProvider = userSettingsProvider;
            this.serializer = serializer;
            this.logger = logger;
        }

        public IRuleSettingsProvider Get(Language language) =>
            new RuleSettingsProvider(activeSolutionBoundTracker, userSettingsProvider, serializer, language, logger);
    }
}
