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

using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.UI;

[TestClass]
public class BindingResultTests
{
    [DynamicData(nameof(ResultValues))]
    [DataTestMethod]
    public void BindingResult_VerificationFailed_ReferToSameInstances(object baseProperty, object actualProperty)
    {
        baseProperty.Should().BeSameAs(actualProperty);
    }

    [DynamicData(nameof(ResultProperties))]
    [DataTestMethod]
    public void BindingResult_HasExpectedProperties(object result, string warningText, bool isSuccessful)
    {
        ((BindingResult)result).ProblemDescription.Should().Be(warningText);
        ((BindingResult)result).IsSuccessful.Should().Be(isSuccessful);
    }

    public static object[][] ResultValues =>
    [
        [BindingResult.ValidationFailure.ConnectionNotFound, BindingResult.ConnectionNotFound],
        [BindingResult.ValidationFailure.SharedConfigurationNotAvailable, BindingResult.SharedConfigurationNotAvailable],
        [BindingResult.ValidationFailure.CredentialsNotFound, BindingResult.CredentialsNotFound],
        [BindingResult.ValidationFailure.ProjectKeyNotFound, BindingResult.ProjectKeyNotFound],
    ];

    public static object[][] ResultProperties =>
    [
        [BindingResult.Success, null, true],
        [BindingResult.Failed, UiResources.FetchingBindingStatusFailedText, false],
        [BindingResult.ConnectionNotFound, UiResources.FetchingBindingStatusFailedTextConnectionNotFound, false],
        [BindingResult.SharedConfigurationNotAvailable, UiResources.FetchingBindingStatusFailedTextNoSharedConfiguration, false],
        [BindingResult.CredentialsNotFound, UiResources.FetchingBindingStatusFailedTextCredentialsNotFound, false],
        [BindingResult.ProjectKeyNotFound, UiResources.FetchingBindingStatusFailedTextProjectNotFound, false],
    ];
}
