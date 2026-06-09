using Microsoft.EntityFrameworkCore;
using GraWat.Data;
using Microsoft.AspNetCore.Identity;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

// Global düzeyde varsayılan kültürü tr-TR (Türkçe / Türkiye) olarak ayarlayalım
var cultureInfo = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

var builder = WebApplication.CreateBuilder(args);

// --- SERVİS AYARLARI ---
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Veritabanı Bağlantıları
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<GraWatContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DİKKAT: RequireConfirmedAccount = false yapıldı! (E-posta onayı istemeden giriş yapılabilmesi için)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// --- HTTP İSTEK YAPILANDIRMASI ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Request Localization Middleware entegrasyonu
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
    SupportedCultures = new[] { cultureInfo },
    SupportedUICultures = new[] { cultureInfo }
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// --- ADMİN ROLÜ VE KULLANICI OLUŞTURMA (EN TEMİZ VE GARANTİ YOL) ---
using (var scope = app.Services.CreateScope())
{
    // Veritabanı tohumlama (Seed Data)
    var grawatContext = scope.ServiceProvider.GetRequiredService<GraWatContext>();
    var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbInitializer.SeedAsync(grawatContext, identityContext).GetAwaiter().GetResult();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // 1. Veritabanında "Admin" rolü yoksa oluştur
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // 2. Admin Maili ve Varsayılan Şifre
    var adminMail = "admin@gmail.com";
    var adminSifre = "Admin65+"; // İlk giriş için geçerli şifreniz (Büyük harf, küçük harf, rakam ve işaret içerir)

    // 3. Veritabanında bu mailde biri var mı diye bakıyoruz
    var adminKullanici = await userManager.FindByEmailAsync(adminMail);

    // 4. Eğer böyle bir kullanıcı YOKSA, sistemi yormadan direkt KENDİSİ OLUŞTURUYOR!
    if (adminKullanici == null)
    {
        adminKullanici = new IdentityUser
        {
            UserName = adminMail,
            Email = adminMail,
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminKullanici, adminSifre);
    }
    else
    {
        // Kulllanıcı zaten var ama şifreyi hatırlamıyorduk, o yüzden Admin65+ olarak güncelliyoruz:
        var token = await userManager.GeneratePasswordResetTokenAsync(adminKullanici);
        await userManager.ResetPasswordAsync(adminKullanici, token, adminSifre);
    }

    // 5. Kullanıcı var (veya yeni oluşturuldu), şimdi ona kesin olarak ADMIN yetkisini ver
    if (!await userManager.IsInRoleAsync(adminKullanici, "Admin"))
    {
        await userManager.AddToRoleAsync(adminKullanici, "Admin");
    }
}
// --- KODLARIN BİTİŞİ ---

app.Run();