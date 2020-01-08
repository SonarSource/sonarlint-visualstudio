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
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class HostedCommandControllerBaseTests
    {
        [TestMethod]
        public void Ctor_InvalidArgument_Throws()
        {
            // Arrange 
            Action act = () => new TestHostedController(null);

            // Act & Assert
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void Ctor_ValidArgument_InitializedCorrectly()
        {
            // Arrange 
            var serviceProvider = new ConfigurableServiceProvider();

            // Act
            var testSubject = new TestHostedController(serviceProvider);

            // Assert
            testSubject.ServiceProvider.Should().BeSameAs(serviceProvider);
        }

        [TestMethod]
        public void Ctor_QueryStatus()
        {
            // Arrange 
            var serviceProvider = new ConfigurableServiceProvider();
            var testSubject = (IOleCommandTarget)new TestHostedController(serviceProvider);
            var guid = Guid.NewGuid();

            // Act
            var result = testSubject.QueryStatus(ref guid, 0, null, IntPtr.Zero);

            // Assert
            result.Should().Be((int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP);
        }

        [TestMethod]
        public void Ctor_QueryExec()
        {
            // Arrange 
            var serviceProvider = new ConfigurableServiceProvider();
            var testSubject = (IOleCommandTarget)new TestHostedController(serviceProvider);
            var guid = Guid.NewGuid();

            // Act
            var result = testSubject.Exec(ref guid, 0, 0, IntPtr.Zero, IntPtr.Zero);

            // Assert
            result.Should().Be((int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP);
        }

        internal class TestHostedController : HostedCommandControllerBase
        {
            public TestHostedController(System.IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }
    }
}
