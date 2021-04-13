/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Interaction logic for GeneralOptionsDialogControl.xaml
    /// </summary>
    public partial class GeneralOptionsDialogControl : UserControl
    {
        private readonly ISonarLintSettings settings;
        private readonly ILogger logger;

        public GeneralOptionsDialogControl(ISonarLintSettings settings, ICommand openSettingsFileCommand, ILogger logger)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (openSettingsFileCommand == null)
            {
                throw new ArgumentNullException(nameof(openSettingsFileCommand));
            }

            this.settings = settings;
            this.logger = logger;

            InitializeComponent();

            this.OpenSettingsButton.Command = openSettingsFileCommand;
        }
    }
}
