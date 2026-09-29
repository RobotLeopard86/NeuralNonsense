//Setup ASP.NET
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
app.UseCors();
app.UseHttpsRedirection();
app.MapHub<NeuralNonsense.NNHub>("/hub");
app.MapPost("/api/join");

//Run
app.Run();