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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingCredentialsLoaderTests
    {
        private Mock<ICredentialStoreService> store;
        private Uri mockUri;
        private SolutionBindingCredentialsLoader testSubject;

        [TestInitialize]
        public void Setup()
        {
            store = new Mock<ICredentialStoreService>();
            mockUri = new Uri("http://sonarsource.com");
            testSubject = new SolutionBindingCredentialsLoader(store.Object);
        }

        [TestMethod]
        public void Ctor_NullStore_Exception()
        {
            Action act = () => new SolutionBindingCredentialsLoader(null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("store");
        }

        [TestMethod]
        public void Load_ServerUriIsNull_Null()
        {
            var actual = testSubject.Load(null);
            actual.Should().Be(null);
        }

        [TestMethod]
        public void Load_NoCredentials_Null()
        {
            store.Setup(x => x.ReadCredentials(mockUri)).Returns(null as Credential);

            var actual = testSubject.Load(mockUri);
            actual.Should().Be(null);
        }

        [TestMethod]
        public void Load_CredentialsExist_CredentialsWithSecuredString()
        {
            var credentials = new Credential("user", "password");
            store
                .Setup(x => x.ReadCredentials(It.Is<TargetUri>(t => t.ActualUri == mockUri)))
                .Returns(credentials);

            var actual = testSubject.Load(mockUri);
            actual.Should().BeEquivalentTo(new BasicAuthCredentials("user", "password".ToSecureString()));
        }

        [TestMethod]
        public void Save_ServerUriIsNull_CredentialsNotSaved()
        {
            var credentials = new BasicAuthCredentials("user", "password".ToSecureString());

            testSubject.Save(credentials, null);

            store.Verify(x=> x.WriteCredentials(It.IsAny<TargetUri>(), It.IsAny<Credential>()), Times.Never);
        }

        [TestMethod]
        public void Save_CredentialsAreNull_CredentialsNotSaved()
        {
            testSubject.Save(null, mockUri);

            store.Verify(x => x.WriteCredentials(It.IsAny<TargetUri>(), It.IsAny<Credential>()), Times.Never);
        }

        [TestMethod]
        public void Save_CredentialsAreNotBasicAuth_CredentialsNotSaved()
        {
            var mockCredentials = new Mock<ICredentials>();
            testSubject.Save(mockCredentials.Object, mockUri);

            store.Verify(x => x.WriteCredentials(It.IsAny<TargetUri>(), It.IsAny<Credential>()), Times.Never);
        }

        [TestMethod]
        public void Save_CredentialsAreBasicAuth_CredentialsSavedWithUnsecuredString()
        {
            var credentials = new BasicAuthCredentials("user", "password".ToSecureString());
            testSubject.Save(credentials, mockUri);

            store.Verify(x => 
                x.WriteCredentials(
                    It.Is<TargetUri>(t => t.ActualUri == mockUri), 
                    It.Is<Credential>(c=> c.Username == "user" && c.Password == "password")), 
                Times.Once);
        }
    }
}
