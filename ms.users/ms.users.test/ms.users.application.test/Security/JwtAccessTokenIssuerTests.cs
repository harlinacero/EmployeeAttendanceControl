using Microsoft.IdentityModel.Tokens;
using Moq;
using ms.users.application.Security;
using ms.users.domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace ms.users.application.test.Security
{
    public class JwtAccessTokenIssuerTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

        private readonly RsaSecurityKey _key;
        private readonly JwtAccessTokenIssuer _issuer;

        public JwtAccessTokenIssuerTests()
        {
            _key = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" };
            var keysMock = new Mock<ISigningKeyProvider>();
            keysMock.SetupGet(keys => keys.Key).Returns(_key);
            var timeMock = new Mock<TimeProvider>();
            timeMock.Setup(time => time.GetUtcNow()).Returns(Now);
            _issuer = new JwtAccessTokenIssuer(keysMock.Object, TestSettings.Create(), timeMock.Object);
        }

        private JwtSecurityToken Issue() =>
            new JwtSecurityTokenHandler().ReadJwtToken(_issuer.Issue(new User { UserName = "jdoe", Role = "admin" }));

        [Fact]
        public void IssueShouldSignWithRs256AndKeyId()
        {
            // Act
            var token = Issue();

            // Assert
            Assert.Equal(SecurityAlgorithms.RsaSha256, token.Header.Alg);
            Assert.Equal("test-key", token.Header.Kid);
        }

        [Fact]
        public void IssueShouldExpireAfterConfiguredMinutes()
        {
            // Act
            var token = Issue();

            // Assert
            Assert.Equal(Now.AddMinutes(15).UtcDateTime, token.ValidTo);
            Assert.Equal(900, _issuer.LifetimeSeconds);
        }

        [Fact]
        public void IssueShouldIncludeNameIdentifierRoleAndIssuer()
        {
            // Act
            var token = Issue();

            // Assert
            Assert.Equal("jdoe", token.Claims.Single(claim => claim.Type == "nameid").Value);
            Assert.Equal("admin", token.Claims.Single(claim => claim.Type == "role").Value);
            Assert.Equal(TestSettings.Issuer, token.Issuer);
        }
    }
}
