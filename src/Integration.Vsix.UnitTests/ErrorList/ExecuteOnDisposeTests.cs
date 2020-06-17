/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class ExecuteOnDisposeTests
    {
        [TestMethod]
        public void Ctor_InvalidArg_Throws()
        {
            Action act = () => new ExecuteOnDispose(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("onDispose");
        }

        [TestMethod]
        public void Dispose_MultipleCalls_OperationExecutedOnlyOnce()
        {
            int count = 0;

            var testSubject = (IDisposable)new ExecuteOnDispose(() => count++);

            testSubject.Dispose();
            count.Should().Be(1);

            testSubject.Dispose();
            count.Should().Be(1);
        }

        [TestMethod]
        public void Dispose_ExceptionsAreNotSuppressed()
        {
            var testSubject = (IDisposable)new ExecuteOnDispose(() => throw new InvalidOperationException("xxx"));

            Action act = () => testSubject.Dispose();

            act.Should().ThrowExactly<InvalidOperationException>().And.Message.Should().Be("xxx");
        }
    }
}
