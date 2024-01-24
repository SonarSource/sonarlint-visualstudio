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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Logging;
using SonarQube.Client.Requests;
using SonarQube.Client.Tests.Infra;

namespace SonarQube.Client.Tests.Requests
{
    [TestClass]
    public class UnversionedRequestFactoryTests
    {
        [TestMethod]
        public void Create_Throws_When_Not_Registered()
        {
            var logger = new TestLogger();
            var factory = new UnversionedRequestFactory(logger);
            Action action = () => factory.Create<ITestRequest>(null);
            action.Should().ThrowExactly<InvalidOperationException>()
                .WithMessage("Could not find factory for 'ITestRequest'.");

            logger.ErrorMessages.Should().Contain(new[] { "Could not find factory for 'ITestRequest'." });
        }

        [TestMethod]
        public void Create_Returns_New_Instance()
        {
            var logger = new TestLogger();
            var factory = new UnversionedRequestFactory(logger);
            factory.RegisterRequest<ITestRequest, TestRequest>();
            factory.RegisterRequest<IAnotherRequest, AnotherRequest>();

            factory.Create<ITestRequest>(null).Should().BeOfType<TestRequest>();
            factory.Create<IAnotherRequest>(null).Should().BeOfType<AnotherRequest>();
        }

        [TestMethod]
        public void Register_Same_RequestInterface_Throws()
        {
            var logger = new TestLogger();
            var factory = new UnversionedRequestFactory(logger);
            factory.RegisterRequest<ITestRequest, TestRequest>();

            Action action = () => factory.RegisterRequest<ITestRequest, TestRequest>();

            action.Should().ThrowExactly<InvalidOperationException>()
                .WithMessage("Registration for ITestRequest already exists.");

            logger.ErrorMessages.Should().Contain(new[] { "Registration for ITestRequest already exists." });
        }

        public interface ITestRequest : IRequest { }

        public interface IAnotherRequest : IRequest { }

        public class TestRequest : ITestRequest
        {
            public ILogger Logger { get; set; }
        }

        public class AnotherRequest : IAnotherRequest
        {
            public ILogger Logger { get; set; }
        }
    }
}
