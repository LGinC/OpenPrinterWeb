using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using OpenPrinterWeb.Services;
using System.Net.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace OpenPrinterWeb.Tests
{
    public class JwtAuthenticationStateProviderTests
    {
        private readonly Mock<IJSRuntime> _mockJsRuntime;
        private readonly Mock<HttpClient> _mockHttpClient;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly JwtAuthenticationStateProvider _provider;

        public JwtAuthenticationStateProviderTests()
        {
            _mockJsRuntime = new Mock<IJSRuntime>();
            _mockHttpClient = new Mock<HttpClient>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _provider = new JwtAuthenticationStateProvider(_mockJsRuntime.Object, _mockHttpClient.Object, _mockHttpContextAccessor.Object);
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ShouldReturnAnonymous_WhenNoToken()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            _mockJsRuntime.Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
                .ReturnsAsync((string?)null);

            // Act
            var state = await _provider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(state.User.Identity);
            Assert.False(state.User.Identity!.IsAuthenticated);
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ShouldReturnAuthenticated_WhenTokenExistsInLocalStorage()
        {
            // Arrange
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
            var token = GenerateTestJwt();
            _mockJsRuntime.Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
                .ReturnsAsync(token);

            // Act
            var state = await _provider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(state.User.Identity);
            Assert.True(state.User.Identity!.IsAuthenticated);
            // JwtSecurityTokenHandler might map ClaimTypes.Name to "unique_name", so Identity.Name might be null if not mapped back.
            // We check for presence of the claim value.
            Assert.Contains(state.User.Claims, c => c.Value == "testuser");
        }

        [Fact]
        public async Task GetAuthenticationStateAsync_ShouldReturnAuthenticated_WhenHttpContextUserIsAuthenticated()
        {
             // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "httpUser") }, "test"));
            var context = new DefaultHttpContext { User = user };
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(context);

            // Act
            var state = await _provider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(state.User.Identity);
            Assert.True(state.User.Identity!.IsAuthenticated);
            Assert.Equal("httpUser", state.User.Identity!.Name);
            // Should verify localStorage was NOT called if HttpContext is used (optimization check, optional)
        }

        private string GenerateTestJwt()
        {
             // Must be at least 32 characters for HMAC-SHA256
             var mySecret = "asdv234234^&%&^%&^hjsdfb2%%%1234567890"; 
             var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));

             var tokenHandler = new JwtSecurityTokenHandler();
             var tokenDescriptor = new SecurityTokenDescriptor
             {
                 Subject = new ClaimsIdentity(new Claim[]
                 {
                     new Claim(ClaimTypes.Name, "testuser"),
                 }),
                 Expires = DateTime.UtcNow.AddDays(7),
                 SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
             };

             var token = tokenHandler.CreateToken(tokenDescriptor);
             return tokenHandler.WriteToken(token);
        }
    }
}
