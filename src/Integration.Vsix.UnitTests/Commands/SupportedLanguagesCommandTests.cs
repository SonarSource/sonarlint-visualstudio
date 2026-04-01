/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using SonarLint.VisualStudio.Integration.SupportedLanguages;
using SonarLint.VisualStudio.Integration.Vsix.Commands;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Commands;

[TestClass]
public class SupportedLanguagesCommandTests
{
    private ISupportedLanguagesWindowService supportedLanguagesWindowService = null!;
    private SupportedLanguagesCommand testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        supportedLanguagesWindowService = Substitute.For<ISupportedLanguagesWindowService>();
        testSubject = new SupportedLanguagesCommand(supportedLanguagesWindowService);
    }

    [TestMethod]
    public void Invoke_CallsShow()
    {
        testSubject.Invoke(CommandHelper.CreateRandomOleMenuCommand(), null);

        supportedLanguagesWindowService.Received(1).Show();
    }
}
