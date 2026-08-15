//Setup ASP.NET
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

//Configure services
builder.Services.AddOpenApi();

//Prepare app
WebApplication app = builder.Build();
if(app.Environment.IsDevelopment()) {
	app.MapOpenApi();
}
app.UseHttpsRedirection();

//Run
app.Run();