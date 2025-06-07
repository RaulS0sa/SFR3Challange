//var builder = WebApplication.CreateBuilder(args);
using SFR3JobScheduler.JssJobHandler;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
services.AddHttpContextAccessor();
builder.Services.AddSignalR();
services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(90);  // Adjust based on your needs
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
});
services.AddSingleton<JssSessionService>(sp =>
{
    return new JssSessionService();
});

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}




app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();


app.Run();

