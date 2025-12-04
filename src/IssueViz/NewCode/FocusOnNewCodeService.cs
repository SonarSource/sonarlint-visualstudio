/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.NewCode;

namespace SonarLint.VisualStudio.IssueVisualization.NewCode;

[Export(typeof(IFocusOnNewCodeService))]
[Export(typeof(IFocusOnNewCodeServiceUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class FocusOnNewCodeService : IFocusOnNewCodeServiceUpdater
{
    private readonly ISonarLintSettings sonarLintSettings;
    private readonly ISLCoreServiceProvider slCoreServiceProvider;
    private readonly IThreadHandling threadHandling;

    [ImportingConstructor]
    public FocusOnNewCodeService(ISonarLintSettings sonarLintSettings,
        IInitializationProcessorFactory initializationProcessorFactory,
        ISLCoreServiceProvider slCoreServiceProvider,
        IThreadHandling threadHandling)
    {
        this.sonarLintSettings = sonarLintSettings;
        this.slCoreServiceProvider = slCoreServiceProvider;
        this.threadHandling = threadHandling;
        InitializationProcessor = initializationProcessorFactory.CreateAndStart<FocusOnNewCodeService>([], () =>
        {
            // sonarLintSettings needs UI thread to initialize settings storage, so the first property access may not be free-threaded
            Current = new(sonarLintSettings.IsFocusOnNewCodeEnabled);
        });
    }

    public IInitializationProcessor InitializationProcessor { get; }
    public FocusOnNewCodeStatus Current { get; private set; } = new(default);

    public void Set(bool isEnabled)
    {
        sonarLintSettings.IsFocusOnNewCodeEnabled = isEnabled;
        Current = new(sonarLintSettings.IsFocusOnNewCodeEnabled);
        NotifySlCoreNewCodeToggled();
        Changed?.Invoke(this, new(Current));
    }

    private void NotifySlCoreNewCodeToggled() =>
        threadHandling.RunOnBackgroundThread(() =>
        {
            if (slCoreServiceProvider.TryGetTransientService(out INewCodeSLCoreService newCodeService))
            {
                newCodeService.DidToggleFocus();
            }
        }).Forget();

    public event EventHandler<NewCodeStatusChangedEventArgs> Changed;
}
