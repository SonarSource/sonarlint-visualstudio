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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    /// <summary>
    /// Composite that coordinates turning server Quality Profile information into a set of config files
    /// for a particular language
    /// </summary>
    [Export(typeof(IBindingConfigProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CompositeBindingConfigProvider : IBindingConfigProvider
    {
        private readonly HashSet<IBindingConfigProvider> providers;

        [ImportingConstructor]
        public CompositeBindingConfigProvider(ISonarQubeService sonarQubeService, ILogger logger)
            : this(
                  new CSharpVBBindingConfigProvider(sonarQubeService, logger),
                  new NonRoslynBindingConfigProvider(sonarQubeService, logger))
        { }

        internal /* for testing */ CompositeBindingConfigProvider(params IBindingConfigProvider[] providers)
        {
            Debug.Assert(providers != null && providers.Length > 0);
            this.providers = new HashSet<IBindingConfigProvider>(providers);
        }

        internal /* for testing */ IEnumerable<IBindingConfigProvider> Providers { get { return this.providers; } }

        #region IBindingConfigProvider methods

        public Task<IBindingConfig> GetConfigurationAsync(SonarQubeQualityProfile qualityProfile, Language language, BindingConfiguration bindingConfiguration, CancellationToken cancellationToken)
        {
            var provider = Providers.FirstOrDefault(p => p.IsLanguageSupported(language));

            if (provider == null)
            {
                throw new ArgumentOutOfRangeException(nameof(language));
            }

            return provider.GetConfigurationAsync(qualityProfile, language, bindingConfiguration, cancellationToken);
        }

        public bool IsLanguageSupported(Language language)
        {
            return Providers.Any(p => p.IsLanguageSupported(language));
        }

        #endregion IBindingConfigProvider methods
    }
}
