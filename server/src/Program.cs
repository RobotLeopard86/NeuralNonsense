using NeuralNonsense;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;

//Setup ASP.NET
Environment.SetEnvironmentVariable("DOTNET_hostBuilder:reloadConfigOnChange", "false");
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//Configure HTTPS certificates
X509Certificate2 cert = X509Certificate2.CreateFromPemFile(Constants.HTTPS_CERT_PATH, Constants.HTTPS_KEY_PATH);
builder.WebHost.ConfigureKestrel(kopts => {
	kopts.ListenAnyIP(6171, lopts => {
		lopts.UseHttps(cert);
	});
});

//Generate JWT key
builder.Services.Configure<KeyManagementOptions>(opts => opts.XmlEncryptor = new NullXmlEncryptor());
SymmetricSecurityKey jwtSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) {
	KeyId = Guid.NewGuid().ToString("N")
};

//Configure services
builder.Services.AddSignalR().AddJsonProtocol(options => {
	options.PayloadSerializerOptions.IncludeFields = true;
});
builder.Services.AddSingleton(jwtSigningKey);
builder.Services.ConfigureHttpJsonOptions(options => {
	options.SerializerOptions.IncludeFields = true;
});
if(builder.Environment.IsDevelopment()) builder.Services.AddCors(options => {
	options.AddDefaultPolicy(builder => {
		builder.WithOrigins("http://" + Constants.DEV_SERVER_HOSTNAME, "https://" + Constants.DEV_SERVER_HOSTNAME).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
	});
});
builder.Services.AddAuthentication().AddJwtBearer(opts => {
	opts.TokenValidationParameters = new TokenValidationParameters {
		ValidateIssuer = true,
		ValidIssuer = Constants.JWT_ISSUER_IDENTITY,
		ValidateAudience = true,
		ValidAudience = Constants.JWT_AUDIENCE_IDENTITY,
		ValidateIssuerSigningKey = true,
		IssuerSigningKey = jwtSigningKey,
		ValidateLifetime = true,
		ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
		ClockSkew = TimeSpan.FromSeconds(30) //SignalR JS client might not notice a disconnect for up to 30 seconds
	};
	opts.Events = new JwtBearerEvents {
		OnMessageReceived = ctx => {
			var token = ctx.Request.Query["access_token"];
			if(!string.IsNullOrEmpty(token) && ctx.Request.Path.StartsWithSegments("/portal/hub")) ctx.Token = token;
			return Task.CompletedTask;
		}
	};
});
builder.Services.AddAuthorization();

//Prepare app
WebApplication app = builder.Build();
if(app.Environment.IsDevelopment()) {
	app.UseHsts();
	app.UseExceptionHandler("/Error");
}
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

//Configure service container
ServiceContainer.instance = new ServiceContainer {
	hubCtx = app.Services.GetRequiredService<IHubContext<NNHub, INNClient>>(),
	badWordChecker = new FakeBadWordCheckerService(),
	gameManager = new GameManagerService(),
	tokenGenerator = new TokenGeneratorService(jwtSigningKey),
	aiGenerator = new FakeAIGeneratorService()
};

//Map routes
app.MapHub<NNHub>("/portal/hub");
app.MapPost("/portal/create", async () => {
	try {
		CreateRoomResponse res = new CreateRoomResponse {
			code = await ServiceContainer.instance.gameManager.CreateRoom(app.Lifetime.ApplicationStopping)
		};
		return Results.Json(res, statusCode: StatusCodes.Status200OK);
	} catch(ClientCausedException e) {
		return Results.BadRequest(e.Message);
	} catch(ServerCausedException e) {
		return Results.InternalServerError(e.Message);
	}
});
app.MapPost("/portal/join", async (JoinRoomRequest req) => {
	try {
		JoinRoomResponse res = await ServiceContainer.instance.gameManager.JoinRoom(req);
		return Results.Json(res, statusCode: StatusCodes.Status200OK);
	} catch(ClientCausedException e) {
		return Results.BadRequest(e.Message);
	} catch(ServerCausedException e) {
		return Results.InternalServerError(e.Message);
	}
});

//Run
app.Run();