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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Http;
using SonarLint.VisualStudio.SLCore.Listener.Http.Model;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation.Http;

[Export(typeof(ISLCoreListener))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class HttpConfigurationListener : IHttpConfigurationListener
{
    private readonly ILogger logger;
    private readonly ICertificateChainValidator chainValidator;
    private readonly ICertificateDtoConverter certificateDtoConverter;

    [ImportingConstructor]
    public HttpConfigurationListener(ILogger logger, ICertificateChainValidator chainValidator, ICertificateDtoConverter certificateDtoConverter)
    {
        this.logger = logger;
        this.chainValidator = chainValidator;
        this.certificateDtoConverter = certificateDtoConverter;
    }

    public Task<SelectProxiesResponse> SelectProxiesAsync(object parameters)
    {
        return Task.FromResult(new SelectProxiesResponse());
    }

    public Task<CheckServerTrustedResponse> CheckServerTrustedAsync(CheckServerTrustedParams parameters)
    {
        logger.WriteLine(SLCoreStrings.HttpConfiguration_ServerTrustVerificationRequest);
        var verificationResult = VerifyChain(parameters.chain);
        logger.WriteLine(SLCoreStrings.HttpConfiguration_ServerTrustVerificationResult, verificationResult);

        return Task.FromResult(new CheckServerTrustedResponse(verificationResult));
    }
    
    private bool VerifyChain(List<X509CertificateDto> chain)
    {
        try
        {
            var primaryCertificate = certificateDtoConverter.Convert(chain.First());
            
            var additionalCertificates = chain.Skip(1).Select(dto =>
            {
                var certificate = certificateDtoConverter.Convert(dto);
                return certificate;
            });
            
            return chainValidator.ValidateChain(primaryCertificate, additionalCertificates);
        }
        catch (Exception e) when (!ErrorHandler.IsCriticalException(e))
        {
            logger.WriteLine(e.ToString());
            return false;
        }
    }
}
