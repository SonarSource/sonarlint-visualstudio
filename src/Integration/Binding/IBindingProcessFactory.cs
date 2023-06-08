﻿/*
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.Binding
{
    /// <summary>
    /// Factory to create a new binding process
    /// </summary>
    internal interface IBindingProcessFactory
    {
        IBindingProcess Create(BindCommandArgs bindingArgs, bool isFirstBinding);
    }

    [Export(typeof(IBindingProcessFactory))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class BindingProcessFactory : IBindingProcessFactory
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly IConfigurationPersister configurationPersister;
        private readonly IExclusionSettingsStorage exclusionSettingsStorage;
        private readonly ILogger logger;

        [ImportingConstructor]
        public BindingProcessFactory(
            ISonarQubeService sonarQubeService,
            IExclusionSettingsStorage exclusionSettingsStorage,
            IConfigurationPersister configurationPersister,
            ILogger logger)
        {
            this.sonarQubeService = sonarQubeService;
            this.configurationPersister = configurationPersister;
            this.exclusionSettingsStorage = exclusionSettingsStorage;
            this.logger = logger;
        }

        public IBindingProcess Create(BindCommandArgs bindingArgs, bool isFirstBinding)
        {
            var bindingConfigProvider = CreateBindingConfigProvider();
            var solutionBindingOp = new SolutionBindingOperation();

            return new BindingProcessImpl(bindingArgs,
                solutionBindingOp,
                bindingConfigProvider,
                exclusionSettingsStorage,
                configurationPersister,
                sonarQubeService,
                logger,
                isFirstBinding);
        }

        private IBindingConfigProvider CreateBindingConfigProvider()
        {
            var cSharpVBBindingConfigProvider = new CSharpVBBindingConfigProvider(sonarQubeService, logger);
            var nonRoslynBindingConfigProvider = new NonRoslynBindingConfigProvider(
                new[]
                {
                    Language.C,
                    Language.Cpp,
                    Language.Js,
                    Language.Ts,
                    Language.Secrets,
                    Language.Css
                }, sonarQubeService, logger);

            return new CompositeBindingConfigProvider(cSharpVBBindingConfigProvider, nonRoslynBindingConfigProvider);
        }
    }
}
