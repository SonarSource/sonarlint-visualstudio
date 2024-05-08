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

using System.ComponentModel.Design;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class CommandInterceptorTests
    {
        [TestMethod]
        public void Exec_CommandProgressionStops_ReturnsZero()
        {
            var guid = new Guid("A057C7F2-E3D3-414F-93C9-014E16122582");
            var commandID = new CommandID(guid, 1);

            bool funcCalled = false;
            var func = () =>
            {
                funcCalled = true;
                return CommandProgression.Stop;
            };

            var testSubject = new CommandInterceptor(commandID, func, new NoOpThreadHandler());

            var result = testSubject.Exec(ref guid, (uint)commandID.ID, 0, IntPtr.Zero, IntPtr.Zero);

            result.Should().Be(0);
            funcCalled.Should().BeTrue();
        }

        [TestMethod]
        public void Exec_CommandProgressionContinue_ReturnsMSOCMDERR_E_FIRST()
        {
            var guid = new Guid("A057C7F2-E3D3-414F-93C9-014E16122582");
            var commandID = new CommandID(guid, 0);

            bool funcCalled = false;
            var func = () =>
            {
                funcCalled = true;
                return CommandProgression.Continue;
            };

            var testSubject = new CommandInterceptor(commandID, func, new NoOpThreadHandler());

            var result = testSubject.Exec(ref guid, (uint)commandID.ID, 0, IntPtr.Zero, IntPtr.Zero);

            result.Should().Be(-2147221248);
            funcCalled.Should().BeTrue();
        }

        [TestMethod]
        public void Exec_CommandIdDoesNotMatch_ReturnsMSOCMDERR_E_FIRST()
        {
            var guid = new Guid("A057C7F2-E3D3-414F-93C9-014E16122582");
            var commandID = new CommandID(guid, 1);

            bool funcCalled = false;
            var func = () =>
            {
                funcCalled = true;
                return CommandProgression.Continue;
            };

            var testSubject = new CommandInterceptor(commandID, func, new NoOpThreadHandler());

            var result = testSubject.Exec(ref guid, nCmdID:(uint)5, 0, IntPtr.Zero, IntPtr.Zero);

            result.Should().Be(-2147221248);
            funcCalled.Should().BeFalse();
        }

        [TestMethod]
        public void Exec_GuidDoesNotMatch_ReturnsMSOCMDERR_E_FIRST()
        {
            var guid = new Guid("A057C7F2-E3D3-414F-93C9-014E16122582");
            var commandID = new CommandID(new Guid("21364CA3-5CA0-41F0-885E-CA2396506132"), 1);

            bool funcCalled = false;
            var func = () =>
            {
                funcCalled = true;
                return CommandProgression.Continue;
            };

            var testSubject = new CommandInterceptor(commandID, func, new NoOpThreadHandler());

            var result = testSubject.Exec(ref guid, (uint)commandID.ID, 0, IntPtr.Zero, IntPtr.Zero);

            result.Should().Be(-2147221248);
            funcCalled.Should().BeFalse();
        }
    }
}
