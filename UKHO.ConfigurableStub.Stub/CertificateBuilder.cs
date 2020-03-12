// British Crown Copyright © 2018,
// All rights reserved.
// 
// You may not copy the Software, rent, lease, sub-license, loan, translate, merge, adapt, vary
// re-compile or modify the Software without written permission from UKHO.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
// OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
// SHALL CROWN OR THE SECRETARY OF STATE FOR DEFENCE BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
// BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
// OF SUCH DAMAGE.

using System;
using System.IO;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace UKHO.ConfigurableStub.Stub
{
    internal class CertificateBuilder
    {
        private const string DefaultSignatureAlgorithm = "SHA256WithRSA";
        private readonly string signatureAlgorithm;
        private const int Strength = 1024;
        private readonly string privateKeyPassword;
        private readonly SecureRandom random = new SecureRandom(new CryptoApiRandomGenerator());
        private readonly X509Name subjectDn = new X509Name("C=UK, CN=UKHO");

        internal CertificateBuilder(string privateKeyPassword, TimeSpan certValidity)
        {
            this.privateKeyPassword = privateKeyPassword;
            signatureAlgorithm = DefaultSignatureAlgorithm;

            var keyGenerationParameters = new KeyGenerationParameters(random, Strength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            SubjectKeyPair = keyPairGenerator.GenerateKeyPair();

            Certificate = BuildX509Certificate(certValidity);
        }

        internal CertificateBuilder(string privateKeyPassword, TimeSpan certValidity, AsymmetricCipherKeyPair subjectKeyPair, string signatureAlgorithm)
        {
            this.privateKeyPassword = privateKeyPassword;
            this.signatureAlgorithm = signatureAlgorithm;
            SubjectKeyPair = subjectKeyPair;
            Certificate = BuildX509Certificate(certValidity);
        }

        internal X509Certificate Certificate { get; }
        internal AsymmetricCipherKeyPair SubjectKeyPair { get; }

        internal MemoryStream CertificateStream
        {
            get
            {
                var store = new Pkcs12Store();
                // What Bouncy Castle calls "alias" is the same as what Windows terms the "friendly name".
                var friendlyName = subjectDn.ToString();
                // Add the certificate.
                var certificateEntry = new X509CertificateEntry(Certificate);
                store.SetCertificateEntry(friendlyName, certificateEntry);

                // Add the private key.
                store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(SubjectKeyPair.Private), new[] { certificateEntry });

                var keyStream = new MemoryStream();
                store.Save(keyStream, privateKeyPassword.ToCharArray(), random);
                keyStream.Position = 0;
                return keyStream;
            }
        }

        internal X509Certificate BuildX509Certificate(TimeSpan certValidity)
        {
            var certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.SetSerialNumber(BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random));
            certificateGenerator.SetSubjectDN(subjectDn);
            certificateGenerator.SetIssuerDN(subjectDn);

            certificateGenerator.SetNotBefore(DateTime.UtcNow.AddSeconds(-1));
            certificateGenerator.SetNotAfter(DateTime.UtcNow.Add(certValidity));

            certificateGenerator.SetPublicKey(SubjectKeyPair.Public);

            var signatureFactory = new Asn1SignatureFactory(signatureAlgorithm, SubjectKeyPair.Private, random);

            var x509Certificate = certificateGenerator.Generate(signatureFactory);
            return x509Certificate;
        }
    }
}