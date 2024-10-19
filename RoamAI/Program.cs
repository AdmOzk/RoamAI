using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoamAI.Context;
using RoamAI.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI();  // Identity UI'yi ekledik

builder.Services.AddControllersWithViews();
builder.Services.AddTransient<ClaudeService>();

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();






var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();


}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();  // Kimlik do�rulamay� ekle

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Welcome}/{action=index}/{id?}");

app.MapRazorPages();  // Razor Pages'i haritalay�n (Identity UI sayfalar� i�in)

app.Run();
