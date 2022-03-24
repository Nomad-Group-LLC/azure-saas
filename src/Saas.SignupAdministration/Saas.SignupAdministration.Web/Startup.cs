using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Saas.SignupAdministration.Web.Data;
using Saas.SignupAdministration.Web.Models;
using Saas.SignupAdministration.Web.Services;
using System;

namespace Saas.SignupAdministration.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString(SR.IdentityDbConnectionProperty)));
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

            services.Configure<IdentityOptions>(options =>
            {
                // Default SignIn settings.
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Default Password settings.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 0;
            });
            var appSettings = Configuration.GetSection(SR.AppSettingsProperty);

            services.Configure<AppSettings>(appSettings);

            services.AddMvc();
            services.AddDistributedMemoryCache();
            services.AddControllersWithViews();
            services.AddScoped<OnboardingWorkflow, OnboardingWorkflow>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
         
            services.AddHttpClient<IAdminServiceClient, AdminServiceClient>()
                .ConfigureHttpClient(client =>
               client.BaseAddress = new Uri(Configuration[SR.AdminServiceBaseUrl]));
          
            services.AddHttpClient<IAdminServiceClient, AdminServiceClient>()
                .ConfigureHttpClient(client =>
               client.BaseAddress = new Uri(Configuration[SR.AdminServiceBaseUrl]));

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(10);
            });

            services.AddApplicationInsightsTelemetry(Configuration[SR.AppInsightsConnectionProperty]);

            services.AddDbContext<SaasSignupAdministrationWebContext>(options =>
                    options.UseSqlServer(Configuration.GetConnectionString("SaasSignupAdministrationWebContext")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(SR.ErrorRoute);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                var adminRoutes = endpoints.MapControllerRoute(
                    name: "Admin",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                var routes = endpoints.MapControllerRoute(name: SR.DefaultName, pattern: SR.MapControllerRoutePattern);
                if (env.IsDevelopment())
                {
                    routes.WithMetadata(new AllowAnonymousAttribute());
                    
                }

                endpoints.MapRazorPages();
            });

            AppHttpContext.Services = app.ApplicationServices;
        }
    }
}
