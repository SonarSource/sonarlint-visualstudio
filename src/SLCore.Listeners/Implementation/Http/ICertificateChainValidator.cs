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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation.Http;

internal interface ICertificateChainValidator
{
    bool ValidateChain(X509Certificate2 primaryCertificate, IEnumerable<X509Certificate2> additionalCertificates);
}

[Export(typeof(ICertificateChainValidator))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class CertificateChainValidator : ICertificateChainValidator
{
    private readonly ILogger logger;

    [ImportingConstructor]
    public CertificateChainValidator(ILogger logger)
    {
        this.logger = logger;
    }

    [ExcludeFromCodeCoverage] // can't easily unit test X509Chain
    public bool ValidateChain(X509Certificate2 primaryCertificate, IEnumerable<X509Certificate2> additionalCertificates)
    {
        logger.LogVerbose("[CertificateChainValidator] Validating certificate: " + primaryCertificate);
        using var x509Chain = new X509Chain();

        foreach (var additionalCertificate in additionalCertificates)
        {
            logger.LogVerbose("[CertificateChainValidator] Using chain certificate: " + primaryCertificate);

            x509Chain.ChainPolicy.ExtraStore.Add(additionalCertificate);
        }

        var validationResult = x509Chain.Build(primaryCertificate);

        if (!validationResult)
        {
            logger.WriteLine(SLCoreStrings.CertificateValidator_Failed);
            foreach (var x509ChainChainStatus in x509Chain.ChainStatus)
            {
                logger.WriteLine(SLCoreStrings.CertificateValidator_FailureReasonTemplate, x509ChainChainStatus.Status, x509ChainChainStatus.StatusInformation);
            }
        }

        return validationResult;
    }
}
