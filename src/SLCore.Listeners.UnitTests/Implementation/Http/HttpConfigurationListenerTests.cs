﻿/*
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
using SonarLint.VisualStudio.SLCore.Listener.Http;
using SonarLint.VisualStudio.SLCore.Listener.Http.Model;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Http;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Http;

[TestClass]
public class HttpConfigurationListenerTests
{
    private ICertificateChainValidator certificateChainValidator;
    private ICertificateDtoConverter certificateDtoConverter;
    private ISystemProxyDetector proxySettingsDetector;
    private TestLogger testLogger;
    private HttpConfigurationListener testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testLogger = new TestLogger();
        certificateChainValidator = Substitute.For<ICertificateChainValidator>();
        certificateDtoConverter = Substitute.For<ICertificateDtoConverter>();
        proxySettingsDetector = Substitute.For<ISystemProxyDetector>();
        testSubject = new HttpConfigurationListener(testLogger, certificateChainValidator, certificateDtoConverter, proxySettingsDetector);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<HttpConfigurationListener, ISLCoreListener>(
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<ICertificateDtoConverter>(),
            MefTestHelpers.CreateExport<ICertificateChainValidator>(),
            MefTestHelpers.CreateExport<ISystemProxyDetector>()
        );

    [TestMethod]
    public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<HttpConfigurationListener>();

    [TestMethod]
    [DataRow("htpp://localhost")]
    [DataRow("https://sonarcloud.io")]
    public async Task SelectProxiesAsync_NoProxyConfigured_ReturnsListWithNoProxyDto(string uri)
    {
        var parameter = new SelectProxiesParams(new Uri(uri));
        MockNoProxyConfigured(parameter.uri);

        var result = await testSubject.SelectProxiesAsync(parameter);

        result.proxies.Should().BeEquivalentTo([ProxyDto.NO_PROXY]);
    }

    [TestMethod]
    public async Task SelectProxiesAsync_UriNull_ReturnsNoProxyDto()
    {
        var parameter = new SelectProxiesParams(null);
        MockNoProxyConfigured(parameter.uri);

        var result = await testSubject.SelectProxiesAsync(parameter);

        result.proxies.Should().BeEquivalentTo([ProxyDto.NO_PROXY]);
    }

    [TestMethod]
    [DataRow("htpp://localhost")]
    [DataRow("https://sonarcloud.io")]
    public async Task SelectProxiesAsync_ProxyConfigured_ReturnsListWithConfiguredProxyDto(string uri)
    {
        var parameter = new SelectProxiesParams(new Uri(uri));
        MockProxyConfigured("http://mycompany.com", 1328);

        var result = await testSubject.SelectProxiesAsync(parameter);

        result.proxies.Should().BeEquivalentTo([new ProxyDto(ProxyType.HTTP, "mycompany.com", 1328)]);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CheckServerTrustedAsync_SingleCertificate_ConvertsAndValidates(bool validationResult)
    {
        var (primaryCertificateDto, primaryCertificate) = SetUpCertificate("some certificate");
        certificateChainValidator.ValidateChain(primaryCertificate, Arg.Is<IEnumerable<X509Certificate2>>(x => !x.Any())).Returns(validationResult);

        var response = await testSubject.CheckServerTrustedAsync(new CheckServerTrustedParams([primaryCertificateDto], "ignored"));

        response.trusted.Should().Be(validationResult);
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task CheckServerTrustedAsync_MultipleCertificates_ConvertsAndValidates(bool validationResult)
    {
        var (primaryCertificateDto, primaryCertificate) = SetUpCertificate("some certificate");
        var (additionalCertificateDto1, additionalCertificate1) = SetUpCertificate("some other certificate 1");
        var (additionalCertificateDto2, additionalCertificate2) = SetUpCertificate("some other certificate 2");
        IEnumerable<X509Certificate2> additionalCertificates = [additionalCertificate1, additionalCertificate2];
        certificateChainValidator
            .ValidateChain(
                primaryCertificate,
                Arg.Is<IEnumerable<X509Certificate2>>(x => x.SequenceEqual(additionalCertificates)))
            .Returns(validationResult);

        var response = await testSubject.CheckServerTrustedAsync(new CheckServerTrustedParams([primaryCertificateDto, additionalCertificateDto1, additionalCertificateDto2], "ignored"));

        response.trusted.Should().Be(validationResult);
    }

    [TestMethod]
    public async Task CheckServerTrustedAsync_Exception_ReturnsFalse()
    {
        var primaryCertificateDto = new X509CertificateDto("some certificate");
        var exceptionReason = "exception reason";
        certificateDtoConverter.Convert(primaryCertificateDto).Throws(new ArgumentException(exceptionReason));
        var response = await testSubject.CheckServerTrustedAsync(new CheckServerTrustedParams([primaryCertificateDto], "ignored"));

        response.trusted.Should().Be(false);
        testLogger.AssertPartialOutputStringExists(exceptionReason);
    }

    private (X509CertificateDto certificateDto, X509Certificate2 certificate) SetUpCertificate(string certificateName)
    {
        var certificateDto = new X509CertificateDto(certificateName);
        var certificate = new X509Certificate2();
        certificateDtoConverter.Convert(certificateDto).Returns(certificate);
        return (certificateDto, certificate);
    }

    private void MockNoProxyConfigured(Uri uri) => proxySettingsDetector.GetProxyUri(Arg.Any<Uri>()).Returns(uri);

    private void MockProxyConfigured(string hostName, int port) => proxySettingsDetector.GetProxyUri(Arg.Any<Uri>()).Returns(new Uri($"{hostName}:{port}"));
}
