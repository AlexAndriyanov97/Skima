using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using API.Auth;
using API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Models.Converters.Users;
using Models.Tokens.Repository;
using Models.Users;
using Models.Users.Repository;

namespace API.Controllers
{
    using Client = global::Client.Models;

    [Route("v1")]
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {
        private readonly UserRepository userRepository;
        private readonly TokenRepository tokenRepository;

        private readonly IAuthenticator authenticator;

        public AuthenticationController(UserRepository userRepository, TokenRepository tokenRepository,
            IAuthenticator authenticator)
        {
            this.userRepository = userRepository;
            this.tokenRepository = tokenRepository;
            this.authenticator = authenticator;
        }

        [HttpPost]
        [Route("users")]
        public async Task<IActionResult> Register([FromBody] Client.Users.UserRegistrationInfo registrationInfo,
            CancellationToken cancellationToken)
        {
            if (registrationInfo == null)
            {
                var error = ServiceErrorResponses.BodyIsMissing("UserRegistrationInfo");
                return this.BadRequest(error);
            }

            User result;
            var creationInfo = new UserCreationInfo(registrationInfo.Login,
                Authenticator.HashPassword(registrationInfo.Password), registrationInfo.FirstName,
                registrationInfo.LastName, registrationInfo.Email, registrationInfo.Phone);

            try
            {
                result = await userRepository.CreateAsync(creationInfo, cancellationToken);
            }
            catch (UserDuplicationException)
            {
                var error = ServiceErrorResponses.ConflictLogin(creationInfo?.Login);
                return this.Conflict(error);
            }

            var clientUser = UserConverter.Convert(result);

            return this.Ok(clientUser);
        }

        [HttpPost]
        [Route("auth/tokens")]
        public async Task<IActionResult> Token([FromBody] Client.Auth.TokenCreationInfo tokenCreationInfo,
            CancellationToken cancellationToken)
        {
            string encodedJwt;

            try
            {
                encodedJwt = await authenticator.AuthenticateAsync(tokenCreationInfo.Login, tokenCreationInfo.Password,
                    cancellationToken);
            }
            catch
            {
                return BadRequest("Invalid login or password");}

            return Ok(encodedJwt);
        }
        
        [HttpDelete]
        [Route("auth/tokens")]
        public async Task<IActionResult> Token([FromQuery] string refreshToken,
            CancellationToken cancellationToken)
        {
            if (refreshToken == null)
            {
                return BadRequest("Refresh token was expected, but received null");
            }

            await tokenRepository.RemoveRefreshTokenAsync(refreshToken);

            return Ok();
        }

        [HttpPatch]
        [Route("auth/tokens")]
        public async Task<IActionResult> Refresh([FromHeader] string token, [FromHeader] string refreshToken)
        {
            var principal = authenticator.GetPrincipalFromExpiredToken(token);
            var userId = principal.Claims.First(claim => claim.Type == "userId").ToString();
            var savedRefreshToken = await tokenRepository.GetRefreshTokenAsync(userId);
            if (savedRefreshToken != refreshToken)
            {
                throw new SecurityTokenException("Invalid refresh token");
            }

            var newJwtToken = GenerateToken(principal.Claims);
            var newRefreshToken = authenticator.GenerateRefreshToken();
            await tokenRepository.RemoveRefreshTokenAsync(userId, refreshToken);
            await tokenRepository.SaveRefreshTokenAsync(userId, newRefreshToken);

            return new ObjectResult(new
            {
                token = newJwtToken,
                refreshToken = newRefreshToken
            });
        }

        private string GenerateToken(IEnumerable<Claim> claims)
        {
            var jwt = new JwtSecurityToken(AuthOptions.ISSUER,
                AuthOptions.AUDIENCE,
                claims,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(AuthOptions.LIFETIME),
                new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
