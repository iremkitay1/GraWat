using Microsoft.EntityFrameworkCore;
using GraWat.Data;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<GraWatContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// --- ADMÝN ROLÜ VE YETKÝLENDÝRME KODLARI BAÞLANGICI ---
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // 1. Veritabanýnda "Admin" rolü yoksa, hemen oluþtur
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // 2. DÝKKAT: Buraya az önce siteye kayýt olurken kullandýðýnýz mail adresini yazýn!
    var adminMail = "admin@gmail.com";

    var adminKullanici = await userManager.FindByEmailAsync(adminMail);

    // 3. Eðer kullanýcýyý bulduysa ve henüz Admin deðilse, ona Admin yetkisini ver
    if (adminKullanici != null && !await userManager.IsInRoleAsync(adminKullanici, "Admin"))
    {
        await userManager.AddToRoleAsync(adminKullanici, "Admin");
    }
}
// --- KODLARIN BÝTÝÞÝ ---

app.Run();