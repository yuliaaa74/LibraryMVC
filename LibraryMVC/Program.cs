using LibraryMVC.Data;
using LibraryMVC.Models;
using LibraryMVC.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using LibraryMVC.FileService;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("DataSource="))
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
}


builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.ClaimsIdentity.UserIdClaimType = System.Security.Claims.ClaimTypes.NameIdentifier;
});


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;

    
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnSigningIn = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal);

        if (user != null)
        {
            var identity = (ClaimsIdentity)context.Principal.Identity;

            var oldClaim = identity.FindFirst("TenantId");
            if (oldClaim != null) identity.RemoveClaim(oldClaim);

            identity.AddClaim(new System.Security.Claims.Claim("TenantId", user.TenantId.ToString()));
        }
    };
    options.Events.OnSignedIn = async context =>
    {
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal);
        if (user != null)
        {
            var identity = (ClaimsIdentity)context.Principal.Identity;
            var oldClaim = identity.FindFirst("TenantId");
            if (oldClaim != null) identity.RemoveClaim(oldClaim);

            identity.AddClaim(new Claim("TenantId", user.TenantId.ToString()));
        }
    };
});
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddHttpClient();
builder.Services.AddTransient<TelegramNotificationService>();
builder.Services.AddTransient<FileService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opt =>
    {
      
        opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        
        opt.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddSignalR();


builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = "473923638947-vibi9cpqcm7sakj1i3ctsekmd5akbh79.apps.googleusercontent.com";
        options.ClientSecret = "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";

        options.Events.OnCreatingTicket = async context =>
        {
            var services = context.HttpContext.RequestServices;
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager = services.GetRequiredService<SignInManager<ApplicationUser>>();
            var db = services.GetRequiredService<AppDbContext>();

            var email = context.Identity.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name = context.Identity.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var googleId = context.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId)) return;

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Name == "Central Library")
                             ?? new Tenant { Name = "Central Library" };
                if (tenant.Id == 0) { db.Tenants.Add(tenant); await db.SaveChangesAsync(); }

                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = name ?? "Google User",
                    EmailConfirmed = true,
                    TenantId = tenant.Id
                };
                await userManager.CreateAsync(user);
            }


            var loginInfo = new UserLoginInfo(context.Scheme.Name, googleId, context.Scheme.Name);
            if (await userManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey) == null)
                await userManager.AddLoginAsync(user, loginInfo);

            await signInManager.SignInAsync(user, isPersistent: false);

            
            var identity = (System.Security.Claims.ClaimsIdentity)context.Principal.Identity;
            var oldClaim = identity.FindFirst("TenantId");
            if (oldClaim != null) identity.RemoveClaim(oldClaim);
            identity.AddClaim(new System.Security.Claims.Claim("TenantId", user.TenantId.ToString()));
        };
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.User);

        if (user != null)
        {
            var identity = (ClaimsIdentity)context.User.Identity;
            var oldClaim = identity.FindFirst("TenantId");
            if (oldClaim != null) identity.RemoveClaim(oldClaim);

            identity.AddClaim(new Claim("TenantId", user.TenantId.ToString()));
        }
    }

    await next();
});

app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapHub<LibraryMVC.Hubs.ReadingListHub>("/readingListHub");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await context.Database.MigrateAsync();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));
    if (!await roleManager.RoleExistsAsync("PremiumUser"))
        await roleManager.CreateAsync(new IdentityRole("PremiumUser"));

    var centralTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == "Central Library")
                        ?? new Tenant { Name = "Central Library" };
    if (centralTenant.Id == 0) { context.Tenants.Add(centralTenant); await context.SaveChangesAsync(); }

    if (await userManager.FindByEmailAsync("admin@library.com") == null)
    {
        var centralAdmin = new ApplicationUser
        {
            UserName = "admin@library.com",
            Email = "admin@library.com",
            Name = "Адміністратор Central Library",
            EmailConfirmed = true,
            TenantId = centralTenant.Id
        };
        var result = await userManager.CreateAsync(centralAdmin, "Admin123!");
        if (result.Succeeded) await userManager.AddToRoleAsync(centralAdmin, "Admin");
    }

   
    var eastTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == "East Library")
                     ?? new Tenant { Name = "East Library" };
    if (eastTenant.Id == 0) { context.Tenants.Add(eastTenant); await context.SaveChangesAsync(); }

    if (await userManager.FindByEmailAsync("admin22@library.com") == null)
    {
        var eastAdmin = new ApplicationUser
        {
            UserName = "admin22@library.com",
            Email = "admin22@library.com",
            Name = "Адміністратор East Library",
            EmailConfirmed = true,
            TenantId = eastTenant.Id
        };
        var result = await userManager.CreateAsync(eastAdmin, "Admin123!");
        if (result.Succeeded) await userManager.AddToRoleAsync(eastAdmin, "Admin");
    }
    
    }





app.Run();
