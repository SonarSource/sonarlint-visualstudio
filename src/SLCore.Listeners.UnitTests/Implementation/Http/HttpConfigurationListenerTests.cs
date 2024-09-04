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

using System.Security.Cryptography.X509Certificates;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Http.Model;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Http;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Http;

[TestClass]
public class HttpConfigurationListenerTests
{
    private HttpConfigurationListener testSubject;
    private TestLogger testLogger;
    private ICertificateChainValidator certificateChainValidator;
    private ICertificateDtoConverter certificateDtoConverter;

    [TestInitialize]
    public void TestInitialize()
    {
        testLogger = new TestLogger();
        certificateChainValidator = Substitute.For<ICertificateChainValidator>();
        certificateDtoConverter = Substitute.For<ICertificateDtoConverter>();
        testSubject = new HttpConfigurationListener(testLogger, certificateChainValidator, certificateDtoConverter);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<HttpConfigurationListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<ICertificateDtoConverter>(),
            MefTestHelpers.CreateExport<ICertificateChainValidator>());
    }

    [TestMethod]
    public void Mef_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<HttpConfigurationListener>();
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(5)]
    [DataRow("something")]
    public async Task SelectProxiesAsync_ReturnsEmptyList(object parameter)
    {
        var result = await testSubject.SelectProxiesAsync(parameter);

        result.proxies.Should().BeEmpty();
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CheckServerTrustedAsync_SingleCertificate_ConvertsAndValidates(bool validationResult)
    {
        var primaryCertificateDto = new X509CertificateDto("some certificate");
        var primaryCertificate = new X509Certificate2();
        certificateDtoConverter.Convert(primaryCertificateDto).Returns(primaryCertificate);
        certificateChainValidator.ValidateChain(primaryCertificate, Arg.Is<IEnumerable<X509Certificate2>>(x => !x.Any())).Returns(validationResult);

        var response = await testSubject.CheckServerTrustedAsync(new([primaryCertificateDto], "ignored"));

        response.trusted.Should().Be(validationResult);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CheckServerTrustedAsync_MultipleCertificates_ConvertsAndValidates(bool validationResult)
    {
        var primaryCertificateDto = new X509CertificateDto("some certificate");
        var additionalCertificateDto1 = new X509CertificateDto("some other certificate 1");
        var additionalCertificateDto2 = new X509CertificateDto("some other certificate 2");
        var primaryCertificate = new X509Certificate2();
        var additionalCertificate1 = new X509Certificate2();
        var additionalCertificate2 = new X509Certificate2();
        certificateDtoConverter.Convert(primaryCertificateDto).Returns(primaryCertificate);
        certificateDtoConverter.Convert(additionalCertificateDto1).Returns(additionalCertificate1);
        certificateDtoConverter.Convert(additionalCertificateDto2).Returns(additionalCertificate2);
        IEnumerable<X509Certificate2> additionalCertificates = [additionalCertificate1, additionalCertificate2];
        certificateChainValidator
            .ValidateChain(
                primaryCertificate,
                Arg.Is<IEnumerable<X509Certificate2>>(x => x.SequenceEqual(additionalCertificates)))
            .Returns(validationResult);

        var response = await testSubject.CheckServerTrustedAsync(new([primaryCertificateDto, additionalCertificateDto1, additionalCertificateDto2], "ignored"));

        response.trusted.Should().Be(validationResult);
    }
    
    [TestMethod]
    public async Task CheckServerTrustedAsync_Exception_ReturnsFalse()
    {
        var primaryCertificateDto = new X509CertificateDto("some certificate");
        var exceptionReason = "exception reason";
        certificateDtoConverter.Convert(primaryCertificateDto).Throws(new ArgumentException(exceptionReason));
        var response = await testSubject.CheckServerTrustedAsync(new([primaryCertificateDto], "ignored"));

        response.trusted.Should().Be(false);
        testLogger.AssertPartialOutputStringExists(exceptionReason);
    }
}
