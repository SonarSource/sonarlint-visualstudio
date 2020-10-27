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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Http;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Http
{
    [TestClass]
    public class OwinPipelineProcessorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var requestHandlersExport = MefTestHelpers.CreateExport<IEnumerable<IOwinPathRequestHandler>>(Array.Empty<IOwinPathRequestHandler>());
            var loggerExport = MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>());

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<OwinPipelineProcessor, IOwinPipelineProcessor>(null, new[] { requestHandlersExport, loggerExport });
        }

        [TestMethod]
        public void Ctor_HandlersAreRegistered()
        {
            var handler1 = CreateHandler("path1/");
            var handler2 = CreateHandler("path2/");

            var testSubject = new OwinPipelineProcessor(new[] { handler1, handler2 }, new TestLogger());

            testSubject.PathToHandlerMap.Count.Should().Be(2);
            testSubject.PathToHandlerMap["path1/"].Should().BeSameAs(handler1);
            testSubject.PathToHandlerMap["path2/"].Should().BeSameAs(handler2);
        }

        private static IOwinPathRequestHandler CreateHandler(string path)
        {
            var handlerMock = new Mock<IOwinPathRequestHandler>();
            handlerMock.Setup(x => x.ApiPath).Returns(path);

            return handlerMock.Object;
        }
    }
}
