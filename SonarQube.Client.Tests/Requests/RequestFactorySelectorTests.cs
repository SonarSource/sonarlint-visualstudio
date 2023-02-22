﻿/*
 * SonarQube Client
 * Copyright (C) 2016-2023 SonarSource SA
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
using SonarQube.Client.Api;
using SonarQube.Client.Requests;
using SonarQube.Client.Tests.Infra;

namespace SonarQube.Client.Tests.Requests
{
    [TestClass]
    public class RequestFactorySelectorTests
    {
        [TestMethod]
        public void Select_IsSonarQube_ReturnsSonarQubeFactory()
        {
            var logger = new TestLogger();
            var testSubject = new RequestFactorySelector();

            var actual = testSubject.Select(isSonarCloud: false, logger);

            // Should be a configure SonarQube request factory
            actual.Should().BeOfType<RequestFactory>();

            var actualRequest = actual.Create<IGetVersionRequest>(new ServerInfo(new Version(6, 7), ServerType.SonarQube));
            actualRequest.Should().NotBeNull();
        }

        [TestMethod]
        public void Select_IsSonarCloud_ReturnsSonarCloudFactory()
        {
            var logger = new TestLogger();
            var testSubject = new RequestFactorySelector();

            var actual = testSubject.Select(isSonarCloud: true, logger);

            actual.Should().BeOfType<UnversionedRequestFactory>();
            var actualRequest = actual.Create<IGetVersionRequest>(new ServerInfo(new Version(8, 0), ServerType.SonarCloud));
            actualRequest.Should().NotBeNull();
        }
    }
}
