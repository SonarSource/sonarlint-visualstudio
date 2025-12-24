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

using System.IO;
using Newtonsoft.Json;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.TestInfrastructure;
using System.Security;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence;

[TestClass]
public class DpapiCurrentUserCredentialsLoaderTests
{
    private IFileSystemService fileSystem;
    private IDpapiProvider dpapiProvider;
    private TestLogger logger;
    private readonly Uri testUri = new("https://server");
    private readonly Uri otherUri = new("https://other-server");
    private readonly string storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SLVS_Credentials", "credentials.json");
    private const string Token = "token123";
    private const string EncryptedToken = "encrypted-token";
    private const string OtherEncryptedToken = "other-encrypted-token";
    private readonly SecureString secureToken = Token.ToSecureString();
    private ITokenCredentials tokenCredentials;
    private DpapiCurrentUserCredentialsLoader testSubject;

    [TestInitialize]
    public void Initialize()
    {
        fileSystem = Substitute.For<IFileSystemService>();
        dpapiProvider = Substitute.For<IDpapiProvider>();
        logger = Substitute.ForPartsOf<TestLogger>();
        testSubject = new DpapiCurrentUserCredentialsLoader(fileSystem, dpapiProvider, logger);

        tokenCredentials = Substitute.For<ITokenCredentials>();
        tokenCredentials.Token.Returns(secureToken);
        dpapiProvider.GetProtectedBase64String(secureToken).Returns(EncryptedToken);
        dpapiProvider.UnprotectBase64String(EncryptedToken).Returns(secureToken.CopyAsReadOnly());
    }

    [TestMethod]
    public void Ctor_SetsLogContext() =>
        logger.Received().ForContext(PersistenceStrings.CredentialsLoader_LogContext, CredentialStoreType.DPAPI.ToString());

    [TestMethod]
    public void StoreType_Dpapi() =>
        testSubject.StoreType.Should().Be(CredentialStoreType.DPAPI);

    [TestMethod]
    public void Load_CredentialsExist_ReturnsTokenAuthCredentials()
    {
        SetUpExistingModel(new()
        {
            [testUri] = new(EncryptedToken)
        });

        var result = testSubject.Load(testUri);

        result.Should().BeOfType<TokenAuthCredentials>();
        ((TokenAuthCredentials)result).Token.ToUnsecureString().Should().Be(Token);
    }

    [TestMethod]
    public void Load_CredentialsNotFound_ReturnsNullAndLogs()
    {
        SetUpExistingModel(new()
        {
            [otherUri] = new(OtherEncryptedToken)
        });

        var result = testSubject.Load(testUri);

        result.Should().BeNull();
        logger.AssertPartialOutputStringExists(string.Format(PersistenceStrings.DpapiCurrentUserCredentialsLoader_NoCredentials, testUri));
    }

    [TestMethod]
    public void Load_StorageFileDoesNotExist_ReturnsNull()
    {
        fileSystem.File.Exists(storagePath).Returns(false);

        var result = testSubject.Load(testUri);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Load_DecryptFails_ReturnsNull()
    {
        SetUpExistingModel(new()
        {
            [testUri] = new(EncryptedToken)
        });
        dpapiProvider.UnprotectBase64String(EncryptedToken).Returns((SecureString)null);

        var result = testSubject.Load(testUri);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Save_TokenCredentials_SavesEncryptedToken()
    {
        SetUpExistingModel(new());

        testSubject.Save(tokenCredentials, testUri);

        VerifyWrittenExpectedModel(new()
        {
            [testUri] = new(EncryptedToken)
        });
    }

    [TestMethod]
    public void Save_WithExistingOtherCredential_PreservesExistingAndAddsNew()
    {
        SetUpExistingModel(new()
        {
            [otherUri] = new(OtherEncryptedToken)
        });

        testSubject.Save(tokenCredentials, testUri);

        VerifyWrittenExpectedModel(new()
        {
            [otherUri] = new(OtherEncryptedToken),
            [testUri] = new(EncryptedToken)
        });
    }

    [TestMethod]
    public void Save_WithExistingSameCredential_Updates()
    {
        SetUpExistingModel(new()
        {
            [otherUri] = new(OtherEncryptedToken),
            [testUri] = new(OtherEncryptedToken),
        });

        testSubject.Save(tokenCredentials, testUri);

        VerifyWrittenExpectedModel(new()
        {
            [otherUri] = new(OtherEncryptedToken),
            [testUri] = new(EncryptedToken)
        });
    }

    [TestMethod]
    public void Save_NotTokenCredentials_ThrowsArgumentException()
    {
        var credentials = Substitute.For<IConnectionCredentials>();
        Action act = () => testSubject.Save(credentials, testUri);

        act.Should().Throw<ArgumentException>().WithMessage("Only token credentials are supported*");
    }

    [TestMethod]
    public void Save_NullCredentials_ThrowsArgumentException()
    {
        Action act = () => testSubject.Save(null, testUri);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void DeleteCredentials_EntryExists_RemovesAndWrites()
    {
        SetUpExistingModel(new()
        {
            [testUri] = new(EncryptedToken)
        });

        testSubject.DeleteCredentials(testUri);

        VerifyWrittenExpectedModel(new());
    }

    [TestMethod]
    public void DeleteCredentials_EntryExists_DoesNotRemoveOtherEntries()
    {
        SetUpExistingModel(new()
        {
            [testUri] = new(EncryptedToken),
            [otherUri] = new(OtherEncryptedToken)
        });

        testSubject.DeleteCredentials(testUri);

        VerifyWrittenExpectedModel(new()
        {
            [otherUri] = new(OtherEncryptedToken)
        });
    }

    [TestMethod]
    public void DeleteCredentials_EntryNotExists_DoesNothing()
    {
        SetUpExistingModel(new()
        {
            [otherUri] = new(OtherEncryptedToken)
        });

        testSubject.DeleteCredentials(testUri);

        fileSystem.File.DidNotReceive().WriteAllText(storagePath, Arg.Any<string>());
    }

    private void SetUpExistingModel(DpapiCurrentUserCredentialsLoader.DpapiCredentialsStorageJsonModel model)
    {
        var existingJson = JsonConvert.SerializeObject(model);
        fileSystem.File.Exists(storagePath).Returns(true);
        fileSystem.File.ReadAllText(storagePath).Returns(existingJson);
    }

    private void VerifyWrittenExpectedModel(DpapiCurrentUserCredentialsLoader.DpapiCredentialsStorageJsonModel expectedModel)
    {
        fileSystem.Directory.Received(1).CreateDirectory(Path.GetDirectoryName(storagePath));
        var writtenJson = fileSystem.File.ReceivedCalls()
            .Single(call => call.GetMethodInfo().Name == "WriteAllText")
            .GetArguments()[1] as string;
        var actualModel = JsonConvert.DeserializeObject<DpapiCurrentUserCredentialsLoader.DpapiCredentialsStorageJsonModel>(writtenJson);
        actualModel.Should().BeEquivalentTo(expectedModel);
    }
}
