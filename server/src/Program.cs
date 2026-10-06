using NeuralNonsense;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography.X509Certificates;

//Setup ASP.NET
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//Configure HTTPS certificates
X509Certificate2 cert = X509Certificate2.CreateFromPemFile("../.certs/cert.pem", "../.certs/dev.pem");
builder.WebHost.ConfigureKestrel(kopts => {
	kopts.ListenAnyIP(6171, lopts => {
		lopts.UseHttps(cert);
	});
});

//Configure services
builder.Services.AddSignalR();
if(builder.Environment.IsDevelopment()) builder.Services.AddCors(options => {
	options.AddDefaultPolicy(builder => {
		builder.WithOrigins("http://localhost:5173", "https://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
	});
});

//Prepare app
WebApplication app = builder.Build();
if(app.Environment.IsDevelopment()) {
	app.UseHsts();
	app.UseExceptionHandler("/Error");
}
app.UseHttpsRedirection();

//Map routes
app.MapHub<NNHub>("/portal/hub");
app.MapPost("/portal/create", async () => {
	try {
		CreateRoomResponse res = new CreateRoomResponse {
			code = await GameManager.instance.CreateRoom()
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
		JoinRoomResponse res = await GameManager.instance.JoinRoom(req.code, req.playerName);
		return Results.Json(res, statusCode: StatusCodes.Status200OK);
	} catch(ClientCausedException e) {
		return Results.BadRequest(e.Message);
	} catch(ServerCausedException e) {
		return Results.InternalServerError(e.Message);
	}
});

//Get hub context
app.Use(async (ctx, next) => {
	GameManager.instance.hubCtx = ctx.RequestServices.GetRequiredService<IHubContext<NNHub>>();
	if(next != null) await next.Invoke();
});

//Run
app.Run();