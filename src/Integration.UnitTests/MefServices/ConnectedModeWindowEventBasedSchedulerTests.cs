/*
 * SonarLint for Visual Studio
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
using Moq;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.MefServices;

[TestClass]
public class ConnectedModeWindowEventBasedSchedulerTests
{
    [TestMethod]
    public void MefCtor_CheckExports()
    {
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeWindowEventBasedScheduler, IConnectedModeWindowEventBasedScheduler>();
        MefTestHelpers.CheckTypeCanBeImported<ConnectedModeWindowEventBasedScheduler, IConnectedModeWindowEventListener>();
    }

    [TestMethod]
    public void MefCtor_CheckMultipleExportsReturnSameInstance()
    {
        MefTestHelpers.CheckMultipleExportsReturnSameInstance<ConnectedModeWindowEventBasedScheduler,
                IConnectedModeWindowEventBasedScheduler,
                IConnectedModeWindowEventListener>();
    }

    [TestMethod]
    public void SubscribeToConnectedModeWindowEvents_SetsHostEventListener()
    {
        var hostMock = new Mock<IHost>();
        var testSubject = CreateTestSubject();
        
        testSubject.SubscribeToConnectedModeWindowEvents(hostMock.Object);
        
        hostMock.VerifyAdd(x => x.ActiveSectionChanged += It.IsAny<EventHandler>());
    }
    
    [TestMethod]
    public void SubscribeToConnectedModeWindowEvents_AlreadySubscribed_Throws()
    {
        var hostMock = new Mock<IHost>();
        var testSubject = CreateTestSubject();
        testSubject.SubscribeToConnectedModeWindowEvents(hostMock.Object);
        
        Action act = () => testSubject.SubscribeToConnectedModeWindowEvents(hostMock.Object);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        var hostMock = new Mock<IHost>();
        var testSubject = CreateTestSubject();
        testSubject.SubscribeToConnectedModeWindowEvents(hostMock.Object);
        
        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();
        
        hostMock.VerifyRemove(x => x.ActiveSectionChanged -= It.IsAny<EventHandler>(), Times.Once);
    }
    
    [TestMethod]
    public void NoEvent_DoesNothing()
    {
        var acted = false;
        var testSubject = CreateTestSubject();
        testSubject.ScheduleActionOnNextEvent(() => { acted = true;});

        acted.Should().BeFalse();
    }

    [TestMethod]
    public void OnEvent_CallsLatestAction()
    {
        var acted1 = false;
        var acted2 = false;
        var testSubject = CreateTestSubject();
        testSubject.ScheduleActionOnNextEvent(() => { acted1 = true;});
        testSubject.ScheduleActionOnNextEvent(() => { acted2 = true;});
        
        testSubject.ActiveSectionChangedListener(null, null);

        acted1.Should().BeFalse();
        acted2.Should().BeTrue();
    }

    [TestMethod]
    public void OnEvent_CallsActionOnlyOnce()
    {
        var calls = 0;
        var testSubject = CreateTestSubject();
        testSubject.ScheduleActionOnNextEvent(() => { calls++;});
        
        testSubject.ActiveSectionChangedListener(null, null);
        testSubject.ActiveSectionChangedListener(null, null);
        testSubject.ActiveSectionChangedListener(null, null);
        testSubject.ActiveSectionChangedListener(null, null);
        testSubject.ActiveSectionChangedListener(null, null);

        calls.Should().Be(1);
    }

    private ConnectedModeWindowEventBasedScheduler CreateTestSubject()
    {
        return new ConnectedModeWindowEventBasedScheduler();
    }
}
