using Microsoft.EntityFrameworkCore;
using GraWat.Data;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// --- SERVÝS AYARLARI ---
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Veritabaný Baðlantýlarý
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<GraWatContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DÝKKAT: RequireConfirmedAccount = false yapýldý! (E-posta onayý istemeden giriþ yapýlabilmesi için)
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// --- HTTP ÝSTEK YAPILANDIRMASI ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

// --- ADMÝN ROLÜ VE KULLANICI OLUÞTURMA (EN TEMÝZ VE GARANTÝ YOL) ---
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // 1. Veritabanýnda "Admin" rolü yoksa oluþtur
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // 2. Admin Maili ve Varsayýlan Þifre
    var adminMail = "admin@gmail.com";
    var adminSifre = "Admin65+"; // Ýlk giriþ için geçerli þifreniz (Büyük harf, küçük harf, rakam ve iþaret içerir)

    // 3. Veritabanýnda bu mailde biri var mý diye bakýyoruz
    var adminKullanici = await userManager.FindByEmailAsync(adminMail);

    // 4. Eðer böyle bir kullanýcý YOKSA, sistemi yormadan direkt KENDÝSÝ OLUÞTURUYOR!
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
        // Kulllanýcý zaten var ama þifreyi hatýrlamýyorduk, o yüzden Admin65+ olarak güncelliyoruz:
        var token = await userManager.GeneratePasswordResetTokenAsync(adminKullanici);
        await userManager.ResetPasswordAsync(adminKullanici, token, adminSifre);
    }

    // 5. Kullanýcý var (veya yeni oluþturuldu), þimdi ona kesin olarak ADMIN yetkisini ver
    if (!await userManager.IsInRoleAsync(adminKullanici, "Admin"))
    {
        await userManager.AddToRoleAsync(adminKullanici, "Admin");
    }
}
// --- KODLARIN BÝTÝÞÝ ---

app.Run();