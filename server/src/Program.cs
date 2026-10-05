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
app.MapHub<NNHub>("/portal/hub");
app.MapPost("/portal/create", async() => {
	string code = await GameManager.instance.CreateRoom();
	return Results.Ok("{\"code\": \"" + code + "\"}");
});

//Get hub context
app.Use(async (ctx, next) => {
	GameManager.instance.hubCtx = ctx.RequestServices.GetRequiredService<IHubContext<NNHub>>();
	if(next != null) await next.Invoke();
});

//Run
app.Run();