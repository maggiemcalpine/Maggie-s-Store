using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using store.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore;

namespace store
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(
                    Configuration.GetConnectionString("DefaultConnection")));
            //services.AddDefaultIdentity<StoreUser>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddIdentity<StoreUser, IdentityRole>()
               .AddDefaultUI()
               .AddRoles<IdentityRole>()
               .AddRoleManager<RoleManager<IdentityRole>>()
               .AddDefaultTokenProviders()
               .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddTransient((s) => {
                return new SendGrid.SendGridClient(Configuration.GetValue<string>("SendGridApiKey"));
            });

            services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>((s) =>
            {
                return new store.Services.EmailSender(s.GetService<SendGrid.SendGridClient>());
            });

            services.AddTransient<Braintree.IBraintreeGateway>((s) => {
                return new Braintree.BraintreeGateway(
                    Configuration.GetValue<string>("Braintree:Environment"),
                    Configuration.GetValue<string>("Braintree:MerchantId"),
                    Configuration.GetValue<string>("Braintree:PublicKey"),
                    Configuration.GetValue<string>("Braintree:PrivateKey")
                );
            });

            services.AddTransient<SmartyStreets.IClient<SmartyStreets.USStreetApi.Lookup>>((s) =>
            {
                SmartyStreets.ClientBuilder builder = new SmartyStreets.ClientBuilder(
                    Configuration.GetValue<string>("SmartyStreets:AuthId"),
                    Configuration.GetValue<string>("SmartyStreets:AuthToken")
                    );
                return builder.BuildUsStreetApiClient();
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
