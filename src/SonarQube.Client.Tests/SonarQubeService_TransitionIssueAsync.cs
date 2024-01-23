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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarQube.Client.Tests;

[TestClass]
public class SonarQubeService_TransitionIssueAsync : SonarQubeService_TestBase
{
    [TestMethod]
    public async Task TransitionIssue_FailedToTransition()
    {
        const string issueKey = "AW9mgJw6eFC3pGl94Wrf";
        var transition = SonarQubeIssueTransition.FalsePositive;
        
        await ConnectToSonarQube("10.4.0.0");
        
        SetupRequest($"api/issues/do_transition?issue={issueKey}&transition={transition.TransitionToLowerCaseString()}",
            "ignored",
            HttpStatusCode.BadGateway);

        var result = await service.TransitionIssueAsync(issueKey, SonarQubeIssueTransition.FalsePositive, "some text", CancellationToken.None);

        result.Should().Be(SonarQubeIssueTransitionResult.FailedToTransition);
        messageHandler.VerifyAll();
        logger.ErrorMessages.Should().Contain("POST api/issues/do_transition request failed.");
        logger.DebugMessages.Should().HaveCountGreaterThan(0);
    }
    
    [TestMethod]
    public async Task TransitionIssue_InsufficientPermissions()
    {
        const string issueKey = "AW9mgJw6eFC3pGl94Wrf";
        var transition = SonarQubeIssueTransition.FalsePositive;
        
        await ConnectToSonarQube("10.4.0.0");
        
        SetupRequest($"api/issues/do_transition?issue={issueKey}&transition={transition.TransitionToLowerCaseString()}",
            "ignored",
            HttpStatusCode.Forbidden);

        var result = await service.TransitionIssueAsync(issueKey, SonarQubeIssueTransition.FalsePositive, "some text", CancellationToken.None);

        result.Should().Be(SonarQubeIssueTransitionResult.InsufficientPermissions);
        messageHandler.VerifyAll();
        logger.WarningMessages.Should().Contain("Insufficient permission to transition the issue.");
    }
    
    [TestMethod]
    public async Task TransitionIssue_CommentAdditionFailed()
    {
        const string issueKey = "AW9mgJw6eFC3pGl94Wrf";
        const string optionalComment = "sometext";
        var transition = SonarQubeIssueTransition.FalsePositive;
        
        await ConnectToSonarQube("10.4.0.0");
        
        SetupRequest($"api/issues/do_transition?issue={issueKey}&transition={transition.TransitionToLowerCaseString()}",
            "ignored");        
        SetupRequest($"api/issues/add_comment?issue={issueKey}&text={optionalComment}",
            "ignored",
            HttpStatusCode.BadGateway);

        var result = await service.TransitionIssueAsync(issueKey, SonarQubeIssueTransition.FalsePositive, optionalComment, CancellationToken.None);

        result.Should().Be(SonarQubeIssueTransitionResult.CommentAdditionFailed);
        messageHandler.VerifyAll();
        logger.ErrorMessages.Should().Contain("POST api/issues/add_comment request failed.");
        logger.DebugMessages.Should().HaveCountGreaterThan(0);
    }
    
    [TestMethod]
    public async Task TransitionIssue_NoComment_NoServerRequest()
    {
        const string issueKey = "AW9mgJw6eFC3pGl94Wrf";
        const string optionalComment = null;
        var transition = SonarQubeIssueTransition.FalsePositive;
        
        await ConnectToSonarQube("10.4.0.0");
        
        SetupRequest($"api/issues/do_transition?issue={issueKey}&transition={transition.TransitionToLowerCaseString()}",
            "ignored");

        var result = await service.TransitionIssueAsync(issueKey, SonarQubeIssueTransition.FalsePositive, optionalComment, CancellationToken.None);

        result.Should().Be(SonarQubeIssueTransitionResult.Success);
        messageHandler.VerifyAll();
    }
    
    [DataTestMethod]
    [DataRow("9.9.0.0", SonarQubeIssueTransition.FalsePositive, SonarQubeIssueTransitionResult.Success)]
    [DataRow("10.2.0.0", SonarQubeIssueTransition.FalsePositive, SonarQubeIssueTransitionResult.Success)]
    [DataRow("10.4.0.0", SonarQubeIssueTransition.FalsePositive, SonarQubeIssueTransitionResult.Success)]
    [DataRow("9.9.0.0", SonarQubeIssueTransition.WontFix, SonarQubeIssueTransitionResult.Success)]
    [DataRow("10.3.0.0", SonarQubeIssueTransition.WontFix, SonarQubeIssueTransitionResult.Success)]
    [DataRow("10.4.0.0", SonarQubeIssueTransition.WontFix, SonarQubeIssueTransitionResult.FailedToTransition)]
    [DataRow("10.6.0.0", SonarQubeIssueTransition.WontFix, SonarQubeIssueTransitionResult.FailedToTransition)]
    [DataRow("9.9.0.0", SonarQubeIssueTransition.Accept, SonarQubeIssueTransitionResult.FailedToTransition)]
    [DataRow("10.3.0.0", SonarQubeIssueTransition.Accept, SonarQubeIssueTransitionResult.FailedToTransition)]
    [DataRow("10.4.0.0", SonarQubeIssueTransition.Accept, SonarQubeIssueTransitionResult.Success)]
    [DataRow("10.6.0.0", SonarQubeIssueTransition.Accept, SonarQubeIssueTransitionResult.Success)]
    public async Task TransitionIssue_SupportedTransitionsTest(string version, SonarQubeIssueTransition transition, SonarQubeIssueTransitionResult expectedResult)
    {
        const string issueKey = "AW9mgJw6eFC3pGl94Wrf";
        const string optionalComment = "sometext";
        
        await ConnectToSonarQube(version);
        
        SetupRequest($"api/issues/do_transition?issue={issueKey}&transition={transition.TransitionToLowerCaseString()}",
            "ignored");        
        SetupRequest($"api/issues/add_comment?issue={issueKey}&text={optionalComment}",
            "ignored");

        var result = await service.TransitionIssueAsync(issueKey, transition, optionalComment, CancellationToken.None);

        result.Should().Be(expectedResult);
    }
    
    [TestMethod]
    public void TransitionIssue_NotConnected()
    {
        // No calls to Connect
        // No need to setup request, the operation should fail

        Func<Task> action = async () => await service.TransitionIssueAsync("key", SonarQubeIssueTransition.Accept, "text", CancellationToken.None);

        action.Should().ThrowExactly<InvalidOperationException>()
            .WithMessage("This operation expects the service to be connected.");

        logger.ErrorMessages.Should().Contain("The service is expected to be connected.");
    }
}
