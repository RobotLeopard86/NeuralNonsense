using NeuralNonsense;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
SymmetricSecurityKey jwtSigningKey = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) {
	KeyId = Guid.NewGuid().ToString("N")
};

//Configure services
builder.Services.AddSignalR();
builder.Services.AddSingleton(jwtSigningKey);
builder.Services.ConfigureHttpJsonOptions(options => {
	options.SerializerOptions.IncludeFields = true;
});
if(builder.Environment.IsDevelopment()) builder.Services.AddCors(options => {
	options.AddDefaultPolicy(builder => {
		builder.WithOrigins("http://" + Constants.DEV_SERVER_HOSTNAME, "https://" + Constants.DEV_SERVER_HOSTNAME).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
	});
});
builder.Services.AddSingleton<IUserIdProvider, SubUserIdProvider>();
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
app.UseAuthentication();
app.UseAuthorization();

//Configure service container
app.Use(async (ctx, next) => {
	ServiceContainer.instance = new ServiceContainer {
		hubCtx = ctx.RequestServices.GetRequiredService<IHubContext<NNHub>>(),
		badWordChecker = new FakeBadWordCheckerService(),
		tokenGenerator = new TokenGeneratorService(jwtSigningKey)
	};
	if(next != null) await next.Invoke();
});

//Map routes
app.MapHub<NNHub>("/portal/hub");
app.MapPost("/portal/create", async () => {
	try {
		CreateRoomResponse res = new CreateRoomResponse {
			code = await GameManager.instance.CreateRoom(app.Lifetime.ApplicationStopping)
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
		JoinRoomResponse res = await GameManager.instance.JoinRoom(req);
		return Results.Json(res, statusCode: StatusCodes.Status200OK);
	} catch(ClientCausedException e) {
		return Results.BadRequest(e.Message);
	} catch(ServerCausedException e) {
		return Results.InternalServerError(e.Message);
	}
});

//Run
app.Run();