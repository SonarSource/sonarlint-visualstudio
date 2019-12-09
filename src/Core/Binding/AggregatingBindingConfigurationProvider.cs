/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.Binding
{
    public class AggregatingBindingConfigurationProvider : IRulesConfigurationProvider
    {
        public AggregatingBindingConfigurationProvider(params IRulesConfigurationProvider[] providers)
        {
            Providers = providers ?? throw new ArgumentNullException(nameof(providers));
            Debug.Assert(providers.All(p => p != null));
        }

        internal /* for testing */ IEnumerable<IRulesConfigurationProvider> Providers { get; }

        #region IRulesConfigurationProvider methods

        public async Task<IRulesConfigurationFile> GetRulesConfigurationAsync(SonarQubeQualityProfile qualityProfile, string organizationKey, Language language, CancellationToken cancellationToken)
        {
            var provider = Providers.FirstOrDefault(p => p.IsLanguageSupported(language));
            Debug.Assert(provider != null, $"Failed to find a provider for language: {language.Name}");

            return await provider?.GetRulesConfigurationAsync(qualityProfile, organizationKey, language, cancellationToken);
        }

        public bool IsLanguageSupported(Language language)
        {
            return Providers.Any(p => p.IsLanguageSupported(language));
        }

        #endregion IRulesConfigurationProvider methods
    }
}
