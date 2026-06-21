using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VRLCRM.Application.Categories;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Invoices;
using VRLCRM.Application.Orders;
using VRLCRM.Application.Payments;
using VRLCRM.Application.StockMovements;
using VRLCRM.Application.Stocks;
using VRLCRM.Application.Suppliers;
using VRLCRM.Application.Users;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Categories;
using VRLCRM.Infrastructure.Customers;
using VRLCRM.Infrastructure.Invoices;
using VRLCRM.Infrastructure.Orders;
using VRLCRM.Infrastructure.Payments;
using VRLCRM.Infrastructure.StockMovements;
using VRLCRM.Infrastructure.Stocks;
using VRLCRM.Infrastructure.Suppliers;
using VRLCRM.Infrastructure.Users;
using VRLCRM.Infrastructure.Data;
using VRLCRM.Infrastructure.Options;

namespace VRLCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.Configure<SeedDataOptions>(configuration.GetSection(SeedDataOptions.SectionName));

        var connectionString = DatabaseConnection.Build(configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISupplierService, SupplierService>();

        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStockMovementService, StockMovementService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IUserService, UserService>();

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Auth/LoginBasic";
            options.AccessDeniedPath = "/Pages/MiscNotAuthorized";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/Shop"))
                {
                    var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                    context.Response.Redirect($"/ShopAuth/Login?returnUrl={returnUrl}");
                }
                else
                {
                    context.Response.Redirect(context.RedirectUri);
                }
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/Shop"))
                    context.Response.Redirect("/ShopAuth/Login");
                else
                    context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        return services;
    }
}
