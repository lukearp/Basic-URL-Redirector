using Basic_URL_Redirector.Data;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.Configure<AppSettingsData>(
    configuration.GetSection("AppSettingsData")
);
builder.Services.AddHostedService<DataLoad>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
//app.MapFallbackToPage("/Redirect");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.Use(async (context, next) =>
{
    var myPagePath = "/Redirect"; // Replace with your page path
    if (context.Request.Path != myPagePath)
    {
        string target = myPagePath + "?originalPath=" + context.Request.Path;
        context.Response.Redirect(target);
        return;
    }

    await next();
});
app.UseAuthorization();

app.MapRazorPages();

app.Run();
