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

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarQube.Client;
using SonarQube.Client.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices
{
    [TestClass]
    public class MefSonarQubeServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<MefSonarQubeService, ISonarQubeService> (
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public async Task ExecutesCallsOnBackgroundThread()
        {
            // Limited check - just that the class does use the threadHandling abstraction
            // to try to execute in the background
            var connectionInfo = new ConnectionInformation(new Uri("http://localhost:123"));

            var threadHandling = new Mock<IThreadHandling>();
            // Assuming the first call is to IGetVersionRequest, which returns a string
            threadHandling.Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<string>>>()))
                .Throws(new IndexOutOfRangeException("this is a test"));


            var testSubject = new MefSonarQubeService(Mock.Of<ILogger>(), threadHandling.Object);

            Func<Task> func = async () => await testSubject.ConnectAsync(connectionInfo, CancellationToken.None);

            func.Should().ThrowExactly<IndexOutOfRangeException>().And
                .Message.Should().Be("this is a test");
        }
    }
}
