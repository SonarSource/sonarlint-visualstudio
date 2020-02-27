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
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class CredentialStoreTests
    {
        private Mock<ICredentialStore> mockCredentialStore;
        private CredentialStore testSubject;
        private TargetUri WellKnownTargetUri;

        [TestInitialize]
        public void TestInitialize()
        {
            mockCredentialStore = new Mock<ICredentialStore>();
            testSubject = new CredentialStore(mockCredentialStore.Object);
            WellKnownTargetUri = new TargetUri("http://sonarcredtest/");
        }

        [TestMethod]
        public void DeleteCreds()
        {
            // Act
            testSubject.DeleteCredentials(WellKnownTargetUri);

            // Assert
            mockCredentialStore.Verify(x => x.DeleteCredentials(
                It.Is<TargetUri>(uri => object.ReferenceEquals(uri, WellKnownTargetUri))), Times.Once);
        }

        [TestMethod]
        public void Write_UserNameAndPassword()
        {
            // Arrange
            var inputCredentials = new Credential("user name", "password");

            // Act
            testSubject.WriteCredentials(WellKnownTargetUri, inputCredentials);

            // Assert
            mockCredentialStore.Verify(x => x.WriteCredentials(
                It.Is<TargetUri>(uri => object.ReferenceEquals(uri, WellKnownTargetUri)),
                It.Is<Credential>(cred => cred.Username == "user name" && cred.Password == "password")), Times.Once);
        }

        [TestMethod]
        public void Read_UserNameAndPassword()
        {
            // Arrange
            var storedCreds = new Credential("user name", "password");

            mockCredentialStore.Setup(x => x.ReadCredentials(It.IsAny<TargetUri>())).Returns(storedCreds);

            // Act
            var actualCreds = testSubject.ReadCredentials(WellKnownTargetUri);

            // Assert
            actualCreds.Should().Be(storedCreds);
            mockCredentialStore.Verify(x => x.ReadCredentials(WellKnownTargetUri), Times.Once);
        }

        [TestMethod]
        public void Write_TokenAsUserName()
        {
            // Arrange
            var inputCredentials = new Credential("token1", String.Empty);

            // Act
            testSubject.WriteCredentials(WellKnownTargetUri, inputCredentials);

            // Assert
            mockCredentialStore.Verify(x => x.WriteCredentials(
                It.Is<TargetUri>(uri => object.ReferenceEquals(uri, WellKnownTargetUri)),
                It.Is<Credential>(cred => cred.Username == CredentialStore.UserNameForTokenCredential  && cred.Password == "token1")), Times.Once);
        }

        [TestMethod]
        public void Read_TokenAsUserName()
        {
            // Arrange
            var storedCreds = new Credential("PersonalAccessToken", "token 2");

            mockCredentialStore.Setup(x => x.ReadCredentials(It.IsAny<TargetUri>())).Returns(storedCreds);

            // Act
            var actualCreds = testSubject.ReadCredentials(WellKnownTargetUri);

            // Assert
            mockCredentialStore.Verify(x => x.ReadCredentials(WellKnownTargetUri), Times.Once);

            actualCreds.Username.Should().Be("token 2");
            actualCreds.Password.Should().BeEmpty();
        }

        [TestMethod]
        public void CredsInOldFormat_RoundTripsSuccessfully()
        {
            // Prior to fixing #768, the token was stored in the user name field.
            // Test we can successfully load and save creds stored in that format.

            // Arrange
            var storedCreds = new Credential("old token", string.Empty);

            mockCredentialStore.Setup(x => x.ReadCredentials(It.IsAny<TargetUri>())).Returns(storedCreds);

            // 1. Load creds from old format -> should be translated to new format
            var loadedCreds1 = testSubject.ReadCredentials(WellKnownTargetUri);
            loadedCreds1.Username.Should().Be("old token");
            loadedCreds1.Password.Should().BeEmpty();

            // 2. Save -> should be in new format
            testSubject.WriteCredentials(WellKnownTargetUri, loadedCreds1);
            mockCredentialStore.Verify(x => x.WriteCredentials(
                It.Is<TargetUri>(uri => object.ReferenceEquals(uri, WellKnownTargetUri)),
                It.Is<Credential>(cred => cred.Username == CredentialStore.UserNameForTokenCredential && cred.Password == "old token")), Times.Once);
        }
    }
}
