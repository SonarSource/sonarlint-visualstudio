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

using SonarLint.VisualStudio.SLCore.Listener.Http.Model;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Http;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests.Implementation.Http;

[TestClass]
public class CertificateDtoConverterTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<CertificateDtoConverter, ICertificateDtoConverter>();
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<CertificateDtoConverter>();
    }

    [TestMethod]
    public void Converts_FromCertificateContent()
    {
        const string certificate =
            """
            -----BEGIN CERTIFICATE-----
            MIIFzTCCA7WgAwIBAgIUMYPipybxIbR2f0Xgjk25Rp3jSugwDQYJKoZIhvcNAQEL
            BQAwZTELMAkGA1UEBhMCQ0gxDzANBgNVBAgMBkdlbmV2YTEQMA4GA1UEBwwHVmVy
            bmllcjEaMBgGA1UECgwRVGVzdCBPcmdhbml6YXRpb24xFzAVBgNVBAMMDnNsdnMu
            dW5pdC50ZXN0MB4XDTI0MDkwNDEwNTAwNloXDTM0MDkwMjEwNTAwNlowZTELMAkG
            A1UEBhMCQ0gxDzANBgNVBAgMBkdlbmV2YTEQMA4GA1UEBwwHVmVybmllcjEaMBgG
            A1UECgwRVGVzdCBPcmdhbml6YXRpb24xFzAVBgNVBAMMDnNsdnMudW5pdC50ZXN0
            MIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEApUBte09V+3W94s9CzmwU
            60OiuTa5K87efL9KV7sAcmSUBRpdGuxWaLtlzX8YhVnIVtzK4lNm0uZa8GXVecfM
            5SYAk3TMpBtKkRwdU6nC8TQ2p/f3CPtMG/x6NK67agLM9aS+pD35ivmr4ogmQoa1
            bXZtQFDwfTbsTsgnHOuYRp0XIYntrDb/fjPTrIL9Xeyc0YqaxJhNii8fckDLLlSb
            8+AOtOw3saM/8ldeTkO92t20oEaxvNQ3ljJQo6Qqmuac+2hd9+WuoqZn05HWfMCN
            q+oBmQsrAYQWxGBwAAZTq5/5X5cYNjnLYDRwv05JZ2TBqqGn8fE1MRSqGZ5MzqE7
            SDrYv0y4nw/GDOqiaD3mMP0lPFZsgGjw4tIyFh5FvnnKLqAsXra3CAQkiztkTBvZ
            dNB4Au1Om1Nd09dkVO9S8KlQlZULwXn6HtCaid+surMoRHM8QKVLjujebu2aPySm
            MPeANOKRMCn936RIWZNrsqakn0b37GVcpGHeJ2FaeK1T+SPfqE0WL8XXDzvPCSI0
            RoLO3thVAivHzriAZzim/j/bQ0zCTH0w+pjKZIld+LGw+6SSczcXw/bxJFkhGC2c
            rE/x/0ztAAMsdEBi3QxFnthwn5U0kph9v6xz5fM8+94W1LLY85aHjZ80UMK5X0Ey
            uyPyhDfJwNYpSqaUH6KZ7NcCAwEAAaN1MHMwCwYDVR0PBAQDAgG2MBMGA1UdJQQM
            MAoGCCsGAQUFBwMBMB0GA1UdDgQWBBRZz1kqxM49KXU60yRk4rXMDgzFSjAfBgNV
            HSMEGDAWgBRZz1kqxM49KXU60yRk4rXMDgzFSjAPBgNVHRMBAf8EBTADAQH/MA0G
            CSqGSIb3DQEBCwUAA4ICAQCeFY7tCIRbkksfRaCISSx8xOvF3+zgcsdewEqV2C/A
            gLdSneIUukjgVwNDLwE12T1gL/Lh7voe6WV7mMgtneNQgmwklhf6HT/8jVDl4ENu
            jC6/zMTII5NTKNf0gRl+39lvDTRDb+RrYh6LyY+uggg1cct1xYUKp/XWa5mi+6E/
            G3JKX20MqdWHXcTwCUWTTX7HmY1rKesMHYBHZjx4FekSmhJvSs3DLt7ceh+98i6J
            MSeST70Kr7rB6GiumKwF5W3v31cvAszV8+YUBSMifhPqIVSD+xZsz+RRk8swh07F
            pvyVJQV4ARtVZ4joXk3PDn5w/iZ3NR+y1PD15MQywNlRATo5KTJZScJ3Nc+ZlN1j
            fQAmNzhyhmUMnmHq1L/I4KJovaDozIMSxCyJVH3LwDcbBCngxlvuKPrmktAmonDm
            k48nd28sSpxvNfNrhCMN8gNUfZjarUWbx5zbX1E0fJTzPgTgcFxPDebIK6l5NBek
            WRo+flJK6TWk23EgdYDG1mNXGhwL5hDIKdhREi9HdxJqFcqjZcpw6Us1TtsubGAg
            f9eTX/tgrtwuj9A195UVulnGpQO80H5UxSoZnF0GhUDZX0peyaZrfEEg2fHMhtxu
            ZCbyvubckbCYdeuUA0yRVGJh6u52GDqP4zaepiBRQGPY1ATr0n6jSYepl6M6hnlX
            zw==
            -----END CERTIFICATE-----
            """;

        var x509Certificate2 = new CertificateDtoConverter().Convert(new X509CertificateDto(certificate));

        x509Certificate2.Subject.Should().Be("CN=slvs.unit.test, O=Test Organization, L=Vernier, S=Geneva, C=CH");
        x509Certificate2.Issuer.Should().Be("CN=slvs.unit.test, O=Test Organization, L=Vernier, S=Geneva, C=CH");
        x509Certificate2.SerialNumber.Should().Be("3183E2A726F121B4767F45E08E4DB9469DE34AE8");
        x509Certificate2.Thumbprint.Should().Be("9AF0EAE02632A1E46DD03D817A084B2CA477B495");
        x509Certificate2.NotAfter.Should().Be(DateTime.Parse("2034-09-02 12:50:06"));
    }
}
