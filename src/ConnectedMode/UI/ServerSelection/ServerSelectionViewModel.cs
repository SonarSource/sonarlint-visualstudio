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

using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.WPF;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection
{
    public class ServerSelectionViewModel : ViewModelBase
    {
        private bool isSonarCloudSelected = true;
        private bool isSonarQubeSelected;
        private string sonarQubeUrl;

        public bool IsSonarCloudSelected
        {
            get => isSonarCloudSelected;
            set
            {
                isSonarCloudSelected = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsNextButtonEnabled));
                RaisePropertyChanged(nameof(ShouldSonarQubeUrlBeFilled));
            }
        } 

        public bool IsSonarQubeSelected 
        {
            get => isSonarQubeSelected;
            set
            {
                isSonarQubeSelected = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsNextButtonEnabled));
                RaisePropertyChanged(nameof(ShouldSonarQubeUrlBeFilled));
            }
        }

        public string SonarQubeUrl
        {
            get => sonarQubeUrl;
            set
            {
                sonarQubeUrl = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsNextButtonEnabled));
                RaisePropertyChanged(nameof(ShouldSonarQubeUrlBeFilled));
                RaisePropertyChanged(nameof(ShowSecurityWarning));
            }
        }

        public bool IsNextButtonEnabled => IsSonarCloudSelected || (IsSonarQubeSelected && IsSonarQubeUrlProvided);
        public bool ShouldSonarQubeUrlBeFilled => IsSonarQubeSelected && !IsSonarQubeUrlProvided;
        private bool IsSonarQubeUrlProvided => !string.IsNullOrWhiteSpace(SonarQubeUrl);
        public bool ShowSecurityWarning => Uri.TryCreate(SonarQubeUrl, UriKind.Absolute, out Uri uriResult) && uriResult.Scheme != Uri.UriSchemeHttps;

        public Connection CreateConnection()
        {
            var url = IsSonarQubeSelected ? SonarQubeUrl : UiResources.SonarCloudUrl;
            var serverType = IsSonarQubeSelected ? ServerType.SonarQube : ServerType.SonarCloud;
            return new Connection(url, serverType, true);
        }
    }
}
