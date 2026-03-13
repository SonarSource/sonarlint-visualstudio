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

using SonarLint.VisualStudio.Integration.Vsix.Commands.DebugMenu;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands.DebugMenu;

[TestClass]
public class DebugMenuCommandTests
{
    [TestMethod]
    public void QueryStatus_MenuIsVisibleAndEnabled()
    {
        var command = CommandHelper.CreateRandomOleMenuCommand();
        var testSubject = new DebugMenuCommand();

        testSubject.QueryStatus(command, null);

#if DEBUG
        command.Visible.Should().BeTrue();
        command.Enabled.Should().BeTrue();
#else
        command.Visible.Should().BeFalse();
        command.Enabled.Should().BeFalse();
#endif
    }
}
