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
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.Core;

[Export(typeof(IFocusOnNewCodeService))]
[Export(typeof(IFocusOnNewCodeServiceUpdater))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class FocusOnNewCodeService : IFocusOnNewCodeServiceUpdater
{
    private readonly ISonarLintSettings sonarLintSettings;

    [ImportingConstructor]
    public FocusOnNewCodeService(ISonarLintSettings sonarLintSettings, IInitializationProcessorFactory initializationProcessorFactory)
    {
        this.sonarLintSettings = sonarLintSettings;
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
        Changed?.Invoke(this, new(Current));
    }

    public event EventHandler<NewCodeStatusChangedEventArgs> Changed;
}
