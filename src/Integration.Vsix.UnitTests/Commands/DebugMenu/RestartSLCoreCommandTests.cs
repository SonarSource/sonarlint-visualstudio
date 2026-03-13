// /*
//  * SonarLint for Visual Studio
//  * Copyright (C) 2016-2025 SonarSource Sàrl
//  * mailto:info AT sonarsource DOT com
//  *
//  * This program is free software; you can redistribute it and/or
//  * modify it under the terms of the GNU Lesser General Public
//  * License as published by the Free Software Foundation; either
//  * version 3 of the License, or (at your option) any later version.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//  * Lesser General Public License for more details.
//  *
//  * You should have received a copy of the GNU Lesser General Public License
//  * along with this program; if not, write to the Free Software Foundation,
//  * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//  */
//
// using SonarLint.VisualStudio.Core;
// using SonarLint.VisualStudio.Integration.Vsix.Commands.DebugMenu;
// using SonarLint.VisualStudio.SLCore;
//
// namespace SonarLint.VisualStudio.Integration.UnitTests.Commands.DebugMenu;
//
// [TestClass]
// public class RestartSLCoreCommandTests
// {
//     private ISLCoreInstanceHandler slCoreInstanceHandler = null!;
//     private IThreadHandling threadHandling = null!;
//     private RestartSLCoreCommand testSubject = null!;
//
//     [TestInitialize]
//     public void TestInitialize()
//     {
//         slCoreInstanceHandler = Substitute.For<ISLCoreInstanceHandler>();
//         threadHandling = Substitute.For<IThreadHandling>();
//         testSubject = new RestartSLCoreCommand(slCoreInstanceHandler, threadHandling);
//     }
//
//     [TestMethod]
//     public void Ctor_NullSLCoreInstanceHandler_Throws()
//     {
//         var act = () => new RestartSLCoreCommand(null, threadHandling);
//
//         act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("slCoreInstanceHandler");
//     }
//
//     [TestMethod]
//     public void Ctor_NullThreadHandling_Throws()
//     {
//         var act = () => new RestartSLCoreCommand(slCoreInstanceHandler, null);
//
//         act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("threadHandling");
//     }
//
//     [TestMethod]
//     public void QueryStatus_CommandIsVisibleAndEnabled()
//     {
//         var command = CommandHelper.CreateRandomOleMenuCommand();
//
//         testSubject.QueryStatus(command, null);
//
//         command.Visible.Should().BeTrue();
//         command.Enabled.Should().BeTrue();
//     }
//
//     [TestMethod]
//     public void Invoke_CallsRestartInstanceAsyncOnBackgroundThread()
//     {
//         var command = CommandHelper.CreateRandomOleMenuCommand();
//
//         testSubject.Invoke(command, null);
//
//         threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
//     }
// }
