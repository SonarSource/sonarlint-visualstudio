/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class AggregatingSolutionBindingCredentialsLoaderTests
{
    private readonly Uri testUri = new("https://server");
    private ISolutionBindingCredentialsLoaderImpl loaderImpl1;
    private ISolutionBindingCredentialsLoaderImpl loaderImpl2;
    private ICredentialStoreTypeProvider credentialStoreTypeProvider;
    private TestLogger logger;
    private CredentialStoreType storeType1;
    private CredentialStoreType storeType2;
    private IConnectionCredentials credentials;
    private AggregatingSolutionBindingCredentialsLoader testSubject;

    [TestInitialize]
    public void Initialize()
    {
        storeType1 = (CredentialStoreType)1;
        storeType2 = (CredentialStoreType)2;
        loaderImpl1 = Substitute.For<ISolutionBindingCredentialsLoaderImpl>();
        loaderImpl1.StoreType.Returns(storeType1);
        loaderImpl2 = Substitute.For<ISolutionBindingCredentialsLoaderImpl>();
        loaderImpl2.StoreType.Returns(storeType2);
        credentialStoreTypeProvider = Substitute.For<ICredentialStoreTypeProvider>();
        logger = Substitute.ForPartsOf<TestLogger>();
        credentials = Substitute.For<IConnectionCredentials>();
        testSubject = new AggregatingSolutionBindingCredentialsLoader(
            [loaderImpl1, loaderImpl2],
            credentialStoreTypeProvider,
            logger);
    }

    [TestMethod]
    public void Ctor_SetsLogContext() =>
        logger.Received().ForContext(PersistenceStrings.CredentialsLoader_LogContext);

    [TestMethod]
    public void Load_ReturnsCredentialsFromCorrectImplementation()
    {
        credentialStoreTypeProvider.CredentialStoreType.Returns(storeType1);
        loaderImpl1.Load(testUri).Returns(credentials);

        var result = testSubject.Load(testUri);

        result.Should().BeSameAs(credentials);
        loaderImpl1.Received(1).Load(testUri);
        loaderImpl2.DidNotReceiveWithAnyArgs().Load(default);
    }

    [TestMethod]
    public void Save_CallsSaveOnCorrectImplementation()
    {
        credentialStoreTypeProvider.CredentialStoreType.Returns(storeType2);

        testSubject.Save(credentials, testUri);

        loaderImpl2.Received(1).Save(credentials, testUri);
        loaderImpl1.DidNotReceiveWithAnyArgs().Save(default, default);
    }

    [TestMethod]
    public void DeleteCredentials_CallsDeleteOnCorrectImplementation()
    {
        credentialStoreTypeProvider.CredentialStoreType.Returns(storeType1);

        testSubject.DeleteCredentials(testUri);

        loaderImpl1.Received(1).DeleteCredentials(testUri);
        loaderImpl2.DidNotReceiveWithAnyArgs().DeleteCredentials(default);
    }

    [TestMethod]
    public void Load_ImplementationThrows_LogsExceptionAndReturnsNull()
    {
        credentialStoreTypeProvider.CredentialStoreType.Returns(storeType1);
        loaderImpl1.When(x => x.Load(testUri)).Do(_ => throw new InvalidOperationException("fail"));

        var result = testSubject.Load(testUri);

        result.Should().BeNull();
        logger.AssertPartialOutputStringExists("fail");
    }

    [TestMethod]
    public void Save_ImplementationThrows_LogsException()
    {
        credentialStoreTypeProvider.CredentialStoreType.Returns(storeType2);
        loaderImpl2.When(x => x.Save(credentials, testUri)).Do(_ => throw new Exception("save error"));

        Action act = () => testSubject.Save(credentials, testUri);

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("save error");
    }

    [TestMethod]
    public void DeleteCredentials_ImplementationThrows_LogsException()
    {
        credentialStoreTypeProvider.CredentialStoreType.Returns(storeType2);
        loaderImpl2.When(x => x.DeleteCredentials(testUri)).Do(_ => throw new Exception("delete error"));

        Action act = () => testSubject.DeleteCredentials(testUri);

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists("delete error");
    }
}
