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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    internal class NonRoslynDummyBindingConfigProvider : IBindingConfigProvider
    {
        // List of languages that use this type of configuration file (all non-Roslyn languages)
        internal static readonly IEnumerable<Language> SupportedLanguages = new []
        {
            Language.C,
            Language.Cpp,
            Language.Js,
            Language.Ts,
            Language.Secrets,
            Language.Css
        };

        private readonly IEnumerable<Language> supportedLanguages;
        public NonRoslynDummyBindingConfigProvider()
            : this(SupportedLanguages)
        { }

        public NonRoslynDummyBindingConfigProvider(IEnumerable<Language> supportedLanguages)
        {
            this.supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
        }

        public bool IsLanguageSupported(Language language) => supportedLanguages.Contains(language);

        public async Task<IBindingConfig> GetConfigurationAsync(SonarQubeQualityProfile qualityProfile, Language language,
            BindingConfiguration bindingConfiguration, CancellationToken cancellationToken)
        {
            if (!IsLanguageSupported(language))
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            return new DummyConfig();
        }

        private class DummyConfig : IBindingConfig
        {
            public void Save()
            {
                // do nothing
            }
        }
    }
}
