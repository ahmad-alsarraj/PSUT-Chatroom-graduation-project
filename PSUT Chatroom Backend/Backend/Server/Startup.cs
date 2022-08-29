using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Server.Db;
using Server.MappingProfiles;
using Server.Services;
using Server.Services.FilesManagers;
using Server.Services.FilesManagers.Cryptography;
using Server.Services.UserSystem;
using Server.Services.WebSockets;

namespace Server
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
            var appOptions = Configuration
                .GetSection(AppOptions.SectionName)
                .Get<AppOptions>();

            services.AddCors()
                .AddScoped<InitializationManager>()
                .AddScoped<UserManager>()
                .AddScoped<RegnewManager>()
                .AddScoped<UserFileManager>()
                .AddScoped<GroupFileManager>()
                .AddScoped<MessageFileManager>()
                .AddScoped<SectionDayConverter>()
                .AddScoped<SaltBae>()
                .AddScoped<BackupManager>()
                .AddScoped<ICryptoTransformFactory<byte[], byte[]>, XorCryptoTransformFactory>()
                .AddSingleton<WebSocketsManager>()
                .AddHttpContextAccessor()
                .AddControllersWithViews()
                .AddFluentValidation(fv =>
                {
                    fv.RegisterValidatorsFromAssemblyContaining<Startup>();
                    fv.ValidatorOptions.CascadeMode = CascadeMode.Stop;
                })
                .AddInvalidModelStateResponseFactory()
                .AddJsonOptions(opts =>
                {
                    var enumConverter = new JsonStringEnumConverter();
                    opts.JsonSerializerOptions.Converters.Add(enumConverter);
                });

            services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UniChatServer", Version = "v1" });
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                })
                .AddAutoMapper(Assembly.GetExecutingAssembly())
                .AddDbContext<AppDbContext>(opt => opt.UseNpgsql(appOptions.BuildAppConnectionString()),
                    contextLifetime: ServiceLifetime.Scoped,
                    optionsLifetime: ServiceLifetime.Singleton)
                .AddDbContextFactory<AppDbContext>(opt => opt.UseNpgsql(appOptions.BuildAppConnectionString()))
                .Configure<AppOptions>(Configuration.GetSection(AppOptions.SectionName));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, InitializationManager initializationManager)
        {
            initializationManager.InitializeSystem().Wait();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage()
                    .UseSwagger()
                    .UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Uni Chat Server v1"));
            }
            app.UseCors(op => op
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin());
            app.UseStaticFiles();
            app.UseWebSockets();

            //app.UseHttpsRedirection();
            app.UseUserSystem()
                .UseAuthorization()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
