//----------------------------------------
// .Net Core WebApi project create script 
//           v10.0.2 from 2025-12-30
//   (C)Robert Grueneis/HTL Grieskirchen 
//----------------------------------------
using ChatterBackend.Hubs;
using GrueneisR.RestClientGenerator;

using Microsoft.OpenApi;

string corsKey = "_myAllowSpecificOrigins";
string swaggerVersion = "v1";
string swaggerTitle = "ChatterBackend";
string restClientFolder = Environment.CurrentDirectory;
string restClientFilename = "_requests.http";

var builder = WebApplication.CreateBuilder(args);

#region -------------------------------------------- ConfigureServices
builder.Services.AddControllers();
builder.Services.AddSingleton<ClientRepository>();
builder.Services.AddSignalR();
builder.Services
  .AddEndpointsApiExplorer()
  .AddAuthorization()
  .AddSwaggerGen(x => x.SwaggerDoc(
    swaggerVersion,
    new OpenApiInfo { Title = swaggerTitle, Version = swaggerVersion }
  ))
  .AddCors(options => options.AddPolicy(
    corsKey,
    x => x.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials()
  ))
  .AddRestClientGenerator(options => options
    .SetFolder(restClientFolder)
    .SetFilename(restClientFilename)
    .SetAction($"swagger/{swaggerVersion}/swagger.json")
  //.EnableLogging()
  );
builder.Services.AddLogging(x => x.AddCustomFormatter());
#endregion

var app = builder.Build();

#region -------------------------------------------- Middleware pipeline
if (app.Environment.IsDevelopment())
{
  app.UseDeveloperExceptionPage();
  Console.ForegroundColor = ConsoleColor.Green;
  Console.WriteLine("++++ Swagger enabled: http://localhost:5000");
  app.UseSwagger();
  Console.WriteLine($@"++++ RestClient generating (after first request) to {restClientFolder}\{restClientFilename}");
  app.UseRestClientGenerator();
  app.UseSwaggerUI(x => x.SwaggerEndpoint($"/swagger/{swaggerVersion}/swagger.json", swaggerTitle));
  Console.ResetColor();
}

app.UseCors(corsKey);
//app.UseHttpsRedirection();
app.UseAuthorization();
#endregion

app.Map("/", () => Results.Redirect("/swagger"));


app.MapControllers();
app.MapHub<ChatHub>("/hub/chat");
Console.WriteLine($"Ready for clients at {DateTime.Now:HH:mm:ss} ...");
app.Run();


