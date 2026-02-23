using SthinkGetDataFromTaxCodeApi.Apis;
using SthinkGetDataFromTaxCodeApi.BootStraping;
using SthinkGetDataFromTaxCodeApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWhen(context =>
{
    return context.Request.Path.StartsWithSegments("/api/v1/auth/token");
}, appBuilder =>
{
    appBuilder.UseMiddleware<IPWhiteListMiddleware>(builder.Configuration["IPWhiteList"]);
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapApplicationApi();
app.Run();

