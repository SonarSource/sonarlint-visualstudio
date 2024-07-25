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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.SLCore.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis;

public interface IUserSettingsUpdater;

[Export(typeof(IUserSettingsUpdater))]
internal sealed class UserSettingsUpdater : IUserSettingsUpdater, IDisposable
{
    private readonly ISLCoreRuleSettingsUpdater slCoreRuleSettings;
    private readonly IUserSettingsProvider userSettingsProvider;
    private readonly IAnalysisRequester analysisRequester;
    
    [ImportingConstructor]
    public UserSettingsUpdater(ILogger logger, ISLCoreRuleSettingsUpdater slCoreRuleSettings, IUserSettingsProvider userSettingsProvider)
        : this(slCoreRuleSettings, userSettingsProvider)
    {
    }

    internal /* for testing */ UserSettingsUpdater(
        ISLCoreRuleSettingsUpdater slCoreRuleSettings, 
        IUserSettingsProvider userSettingsProvider)
    {
        this.userSettingsProvider = userSettingsProvider ?? throw new ArgumentNullException(nameof(userSettingsProvider));
        this.slCoreRuleSettings = slCoreRuleSettings ?? throw new ArgumentNullException(nameof(slCoreRuleSettings)); 

        userSettingsProvider.SettingsChanged += OnSettingsChanged;
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
        slCoreRuleSettings.UpdateStandaloneRulesConfiguration();
        analysisRequester.RequestAnalysis();
    }
    
    public void Dispose()
    {
        // todo
    }
}
