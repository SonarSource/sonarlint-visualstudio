/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

namespace SonarLint.VisualStudio.ConnectedMode.UI;

internal class BindingResult
{
    public static BindingResult Success { get; } = new(null, true);
    public static BindingResult Failed { get; } = new(UiResources.FetchingBindingStatusFailedText, false);
    public static ValidationFailure ConnectionNotFound => ValidationFailure.ConnectionNotFoundFailure;
    public static ValidationFailure CredentialsNotFound => ValidationFailure.CredentialsNotFoundFailure;
    public static ValidationFailure ProjectKeyNotFound => ValidationFailure.ProjectKeyNotFoundFailure;

    private BindingResult(string problemDescription, bool isSuccessful)
    {
        ProblemDescription = problemDescription;
        IsSuccessful = isSuccessful;
    }

    internal class ValidationFailure : BindingResult
    {
        internal static readonly ValidationFailure ConnectionNotFoundFailure = new(UiResources.FetchingBindingStatusFailedTextConnectionNotFound);
        internal static readonly ValidationFailure CredentialsNotFoundFailure = new(UiResources.FetchingBindingStatusFailedTextCredentialsNotFound);
        internal static readonly ValidationFailure ProjectKeyNotFoundFailure = new(UiResources.FetchingBindingStatusFailedTextProjectNotFound);

        private ValidationFailure(string problemDescription) : base(problemDescription, false) { }
    }

    public bool IsSuccessful { get; }
    public string ProblemDescription { get; }
}
