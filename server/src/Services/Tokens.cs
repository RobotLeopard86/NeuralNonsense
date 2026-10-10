using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace NeuralNonsense {
	public sealed class TokenGeneratorService(SymmetricSecurityKey jwtSigningKey) {
		private readonly JsonWebTokenHandler jwtHandler = new JsonWebTokenHandler();
		private readonly SigningCredentials credentials = new SigningCredentials(jwtSigningKey, SecurityAlgorithms.HmacSha256);


		public string GenToken(string room, Room.Member member) {
			return jwtHandler.CreateToken(new SecurityTokenDescriptor {
				Subject = new ClaimsIdentity([
					new Claim("sub", member.uuid.ToString()),
					new Claim("room", room),
					new Claim("name", member.name)
				]),
				Issuer = Constants.JWT_ISSUER_IDENTITY,
				Audience = Constants.JWT_AUDIENCE_IDENTITY,
				Expires = DateTime.UtcNow.AddHours(12),
				SigningCredentials = credentials,
			});
		}
	}

	//Small helper to deal with JWT field name conversion for SignalR
	public sealed class SubUserIdProvider : IUserIdProvider {
		public string? GetUserId(HubConnectionContext c) => c.User?.FindFirst("sub")?.Value;
	}
}