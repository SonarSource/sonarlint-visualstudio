/*
 * SonarQube Client
 * Copyright (C) 2016-2018 SonarSource SA
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

namespace SonarQube.Client.Tests.Services
{
    [TestClass]
    public class RequestFactoryTests
    {
        [TestMethod]
        public void Create_Throws_When_Not_Registered()
        {
            var factory = new RequestFactory();
            Action action = () => factory.Create<ITestRequest>(new Version(1, 0, 0));
            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("Could not find factory for 'ITestRequest'.");
        }

        [TestMethod]
        public void Create_Throws_For_Not_Supported_Versions()
        {
            var factory = new RequestFactory();
            factory.RegisterRequest<ITestRequest, TestRequest1>("2.0.0");

            Action action = () => factory.Create<ITestRequest>(new Version(1, 0, 0));
            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("Could not find compatible implementation of 'ITestRequest' for SonarQube 1.0.0.");
        }

        [TestMethod]
        public void Create_Returns_New_Instance_For_Supported_Versions()
        {
            var factory = new RequestFactory();
            factory.RegisterRequest<ITestRequest, TestRequest1>("1.0.0");
            factory.RegisterRequest<ITestRequest, TestRequest2>("2.0.0");
            factory.RegisterRequest<ITestRequest, TestRequest3>("3.0.0");

            factory.Create<ITestRequest>(new Version(1, 0, 0)).Should().BeOfType<TestRequest1>();
            factory.Create<ITestRequest>(new Version(1, 99, 99)).Should().BeOfType<TestRequest1>();
            factory.Create<ITestRequest>(new Version(2, 0, 0)).Should().BeOfType<TestRequest2>();
            factory.Create<ITestRequest>(new Version(2, 99, 99)).Should().BeOfType<TestRequest2>();
            factory.Create<ITestRequest>(new Version(3, 0, 0)).Should().BeOfType<TestRequest3>();
            factory.Create<ITestRequest>(new Version(99, 0, 0)).Should().BeOfType<TestRequest3>();
        }

        [TestMethod]
        public void Create_Returns_Correct_Types()
        {
            var factory = new RequestFactory();
            factory.RegisterRequest<ITestRequest, TestRequest1>("1.0.0");
            factory.RegisterRequest<IAnotherRequest, AnotherRequest1>("1.0.0");

            factory.Create<IAnotherRequest>(new Version(1, 0, 0)).Should().BeOfType<AnotherRequest1>();
            factory.Create<ITestRequest>(new Version(1, 0, 0)).Should().BeOfType<TestRequest1>();
        }

        [TestMethod]
        public void Create_Null_Returns_Latest_Registered_Version()
        {
            var factory = new RequestFactory();
            factory.RegisterRequest<ITestRequest, TestRequest1>("1.0.0");
            factory.RegisterRequest<ITestRequest, TestRequest2>("2.0.0");

            factory.Create<ITestRequest>(null).Should().BeOfType<TestRequest2>();

            factory.RegisterRequest<ITestRequest, TestRequest3>("3.0.0");

            factory.Create<ITestRequest>(null).Should().BeOfType<TestRequest3>();
        }

        [TestMethod]
        public void Register_Same_RequestInterface_Same_Version_Throws()
        {
            var factory = new RequestFactory();
            factory.RegisterRequest<ITestRequest, TestRequest1>("1.0.0");

            Action action = () => factory.RegisterRequest<ITestRequest, TestRequest2>("1.0.0");

            action.Should().ThrowExactly<InvalidOperationException>().And
                .Message.Should().Be("Registration for ITestRequest with version 1.0.0 already exists.");
        }

        [TestMethod]
        public void Register_Invalid_Version_Throws()
        {
            var factory = new RequestFactory();

            Action action = () => factory.RegisterRequest<ITestRequest, TestRequest2>("asdasd");

            action.Should().ThrowExactly<ArgumentException>().And
                .ParamName.Should().Be("version");
        }

        public interface ITestRequest : IRequest
        {
        }

        public interface IAnotherRequest : IRequest
        {
        }

        public class TestRequest1 : ITestRequest
        {
        }

        public class TestRequest2 : ITestRequest
        {
        }

        public class TestRequest3 : ITestRequest
        {
        }

        public class AnotherRequest1 : IAnotherRequest
        {
        }
    }
}
