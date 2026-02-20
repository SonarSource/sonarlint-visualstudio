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

using SonarLint.VisualStudio.Integration.Vsix.Events;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.UnitTests.Events;

[TestClass]
public class ErrorNotificationDialogViewModelTests
{
    [TestMethod]
    public void Ctor_SetsMessage()
    {
        var testSubject = new ErrorNotificationDialogViewModel(5);

        testSubject.Message.Should().Be(string.Format(Strings.BuildEventNotifier_IssuesFoundMessage, 5));
    }

    [TestMethod]
    public void DoNotShowAgain_DefaultIsFalse()
    {
        var testSubject = new ErrorNotificationDialogViewModel(1);

        testSubject.DoNotShowAgain.Should().BeFalse();
    }

    [TestMethod]
    public void DoNotShowAgain_SetAndGet()
    {
        var testSubject = new ErrorNotificationDialogViewModel(1);

        testSubject.DoNotShowAgain = true;

        testSubject.DoNotShowAgain.Should().BeTrue();
    }
}
