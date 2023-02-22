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
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarQube.Client.Tests.Models.ServerSentEvents
{
    [TestClass]
    public class IssueChangedServerEventTests
    {
        [TestMethod]
        public void Ctor_InvalidIssuesList_Throws()
        {
            Action act = () => { new IssueChangedServerEvent("MyProject", false, null); };

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("branchAndIssueKeys");
        }

        [TestMethod]
        public void Ctor_InvalidProjectKey_Throws()
        {
            Action act = () => { new IssueChangedServerEvent(null, false, new IBranchAndIssueKey[]{new BranchAndIssueKey("i", "b")}); };

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("projectKey");
        }
    }
}
