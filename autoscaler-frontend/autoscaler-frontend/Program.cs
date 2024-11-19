using autoscaler_frontend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection(); // why is this default?
app.UseStaticFiles();
app.UseRouting();

ArgumentParser.SetArgs(args);

Forecaster.Singleton.Start();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();