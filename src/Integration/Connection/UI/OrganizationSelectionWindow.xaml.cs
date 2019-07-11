/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Connection.UI
{
    /// <summary>
    /// Interaction logic for OrganizationSelectionWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage] // Rely on Visual Studio elements that cannot be rendered under test
    public partial class OrganizationSelectionWindow
    {
        public SonarQubeOrganization Organization { get; private set; }

        internal OrganizationSelectionWindow(IEnumerable<SonarQubeOrganization> organizations)
        {
            InitializeComponent();

            var sortedOrganizations = organizations.OrderBy(x => x.Name).ToList();
            OrganizationComboBox.ItemsSource = sortedOrganizations;
        }

        private void OnOwnOkButtonClick(object sender, RoutedEventArgs e)
        {
            Organization = OrganizationComboBox?.SelectedItem as SonarQubeOrganization;
            Debug.Assert(Organization != null,
                "Not expecting organization to be null: user should not have been able to click ok with an invalid selection");

            // Close dialog in the affirmative
            this.DialogResult = true;
        }

        private void OnOtherOrgOkButtonClick(object sender, RoutedEventArgs e)
        {
            var orgKey = IsValidOrganisationKeyConverter.GetTrimmedKey(OrganizationKeyTextBox.Text);

            Debug.Assert(!string.IsNullOrWhiteSpace(orgKey),
                "Not expecting orgKey to be null: user should not have been able to click ok with an invalid orgKey");

            // We don't know the name of the organization at this point so we'll just use the key.
            // It will be displayed in our UI in the Team Explorer, but that's ok for now.
            Organization = new SonarQubeOrganization(orgKey, orgKey);

            // Close dialog in the affirmative
            this.DialogResult = true;
        }
    }
}
