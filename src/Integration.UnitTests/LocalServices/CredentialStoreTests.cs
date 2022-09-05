﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.ComponentModel;
using System.IO;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Integration.UnitTests.LocalServices
{
    [TestClass]
    public class CredentialStoreTests
    {
        private const int MockWin32ErrorCode = 12345678;

        private Mock<ICredentialStore> mockCredentialStore;
        private TestLogger logger;
        private CredentialStore testSubject;
        private TargetUri wellKnownTargetUri;

        [TestInitialize]
        public void TestInitialize()
        {
            mockCredentialStore = new Mock<ICredentialStore>();
            logger = new TestLogger();
            testSubject = new CredentialStore(mockCredentialStore.Object, logger);
            wellKnownTargetUri = new TargetUri("http://sonarcredtest/");
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CredentialStore, ICredentialStoreService>(
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void DeleteCreds()
        {
            // Act
            testSubject.DeleteCredentials(wellKnownTargetUri);

            // Assert
            mockCredentialStore.Verify(x => x.DeleteCredentials(
                It.Is<TargetUri>(uri => object.ReferenceEquals(uri, wellKnownTargetUri))), Times.Once);
        }

        [TestMethod]
        public void Write_UserNameAndPassword()
        {
            // Arrange
            var inputCredentials = new Credential("user name", "password");

            // Act
            testSubject.WriteCredentials(wellKnownTargetUri, inputCredentials);

            // Assert
            mockCredentialStore.Verify(x => x.WriteCredentials(
                It.Is<TargetUri>(uri => object.ReferenceEquals(uri, wellKnownTargetUri)),
                It.Is<Credential>(cred => cred.Username == "user name" && cred.Password == "password")), Times.Once);
        }

        [TestMethod]
        public void Read_NullCredsFromStore_NullReturned()
        {
            mockCredentialStore
                .Setup(x => x.ReadCredentials(It.IsAny<TargetUri>()))
                .Returns(null as Credential);

            var actualCreds = testSubject.ReadCredentials(wellKnownTargetUri);

            actualCreds.Should().BeNull();
        }

        [TestMethod]
        public void Read_UserNameAndPassword()
        {
            // Arrange
            var storedCreds = new Credential("user name", "password");

            mockCredentialStore.Setup(x => x.ReadCredentials(It.IsAny<TargetUri>())).Returns(storedCreds);

            // Act
            var actualCreds = testSubject.ReadCredentials(wellKnownTargetUri);

            // Assert
            actualCreds.Should().Be(storedCreds);
            mockCredentialStore.Verify(x => x.ReadCredentials(wellKnownTargetUri), Times.Once);
        }

        [TestMethod]
        public void Write_CredsAreNull_NullCredsAreWritten()
        {
            testSubject.WriteCredentials(wellKnownTargetUri, null);

            mockCredentialStore.Verify(x=> x.WriteCredentials(wellKnownTargetUri, null), Times.Once);
        }

        [TestMethod]
        public void Write_TokenAsUserName()
        {
            // Arrange
            var inputCredentials = new Credential("token1", String.Empty);

            // Act
            testSubject.WriteCredentials(wellKnownTargetUri, inputCredentials);

            // Assert
            mockCredentialStore.Verify(x => x.WriteCredentials(
                It.Is<TargetUri>(uri => object.ReferenceEquals(uri, wellKnownTargetUri)),
                It.Is<Credential>(cred => cred.Username == CredentialStore.UserNameForTokenCredential  && cred.Password == "token1")), Times.Once);
        }

        [TestMethod]
        public void Read_TokenAsUserName()
        {
            // Arrange
            var storedCreds = new Credential("PersonalAccessToken", "token 2");

            mockCredentialStore.Setup(x => x.ReadCredentials(It.IsAny<TargetUri>())).Returns(storedCreds);

            // Act
            var actualCreds = testSubject.ReadCredentials(wellKnownTargetUri);

            // Assert
            mockCredentialStore.Verify(x => x.ReadCredentials(wellKnownTargetUri), Times.Once);

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
            var loadedCreds1 = testSubject.ReadCredentials(wellKnownTargetUri);
            loadedCreds1.Username.Should().Be("old token");
            loadedCreds1.Password.Should().BeEmpty();

            // 2. Save -> should be in new format
            testSubject.WriteCredentials(wellKnownTargetUri, loadedCreds1);
            mockCredentialStore.Verify(x => x.WriteCredentials(
                It.Is<TargetUri>(uri => object.ReferenceEquals(uri, wellKnownTargetUri)),
                It.Is<Credential>(cred => cred.Username == CredentialStore.UserNameForTokenCredential && cred.Password == "old token")), Times.Once);
        }

        [TestMethod]
        public void DeleteCredentials_Win32Exception_ExceptionIsRethrownAndLogged()
        {
            SetupStoreThrowsWin32Exception(x => x.DeleteCredentials(wellKnownTargetUri));

             Action action = () => testSubject.DeleteCredentials(wellKnownTargetUri);
            action.Should().ThrowExactly<Win32Exception>();

            VerifyWin32ExceptionIsLogged("delete");
        }

        [TestMethod]
        public void DeleteCredentials_RegularException_ExceptionIsRethrownAndNotLogged()
        {
            mockCredentialStore
                .Setup(x => x.DeleteCredentials(wellKnownTargetUri))
                .Throws(new FileFormatException());

            Action action = () => testSubject.DeleteCredentials(wellKnownTargetUri);
            action.Should().ThrowExactly<FileFormatException>();

            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void ReadCredentials_Win32Exception_ExceptionIsRethrownAndLogged()
        {
            SetupStoreThrowsWin32Exception(x => x.ReadCredentials(wellKnownTargetUri));

            Action action = () => testSubject.ReadCredentials(wellKnownTargetUri);
            action.Should().ThrowExactly<Win32Exception>();

            VerifyWin32ExceptionIsLogged("read");
        }

        [TestMethod]
        public void ReadCredentials_RegularException_ExceptionIsRethrownAndNotLogged()
        {
            mockCredentialStore
                .Setup(x => x.ReadCredentials(wellKnownTargetUri))
                .Throws(new FileFormatException());

            Action action = () => testSubject.ReadCredentials(wellKnownTargetUri);
            action.Should().ThrowExactly<FileFormatException>();

            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void WriteCredentials_Win32Exception_ExceptionIsRethrownAndLogged()
        {
            SetupStoreThrowsWin32Exception(x => x.WriteCredentials(wellKnownTargetUri, It.IsAny<Credential>()));

            Action action = () => testSubject.WriteCredentials(wellKnownTargetUri, new Credential("old token", string.Empty));
            action.Should().ThrowExactly<Win32Exception>();

            VerifyWin32ExceptionIsLogged("write");
        }

        [TestMethod]
        public void WriteCredentials_RegularException_ExceptionIsRethrownAndNotLogged()
        {
            mockCredentialStore
                .Setup(x => x.WriteCredentials(wellKnownTargetUri, It.IsAny<Credential>()))
                .Throws(new FileFormatException());

            Action action = () => testSubject.WriteCredentials(wellKnownTargetUri, new Credential("old token", string.Empty));
            action.Should().ThrowExactly<FileFormatException>();

            logger.AssertNoOutputMessages();
        }

        private void SetupStoreThrowsWin32Exception(Expression<Action<ICredentialStore>> expression)
        {
            mockCredentialStore
                .Setup(expression)
                .Throws(new Win32Exception(MockWin32ErrorCode));
        }

        private void VerifyWin32ExceptionIsLogged(string expectedActionName)
        {
            logger.AssertPartialOutputStringExists($"Failed to {expectedActionName} credentials");
            logger.AssertPartialOutputStringExists($"Win32ErrorCode: {MockWin32ErrorCode}");
        }
    }
}
