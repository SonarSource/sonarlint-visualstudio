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
using System.Net.Http;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarQube.Client.Api;

namespace SonarQube.Client.Tests.Api
{
    [TestClass]
    public class SonarQubeService_Ctor
    {
        [TestMethod]
        public void SonarQubeService_Ctor_ArgumentChecks()
        {
            Action action;

            action = () => new SonarQubeService(null, new RequestFactory(), string.Empty);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("messageHandler");

            action = () => new SonarQubeService(new Mock<HttpClientHandler>().Object, null, string.Empty);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("requestFactory");

            action = () => new SonarQubeService(new Mock<HttpClientHandler>().Object, new RequestFactory(), null);
            action.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("userAgent");
        }
    }
}
