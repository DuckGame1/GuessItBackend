
using GuessItAPI.Context;
using GuessItAPI.Jwt;
using GuessItAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GuessItAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var services = builder.Services;
            var configuration = builder.Configuration;

            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "GuessIt API",
                    Version = "v1"
                });
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
                options.OrderActionsBy(apiDesc => $"{apiDesc.HttpMethod}_{apiDesc.RelativePath}");
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtProvider.options.SecretKey))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["good_cookie"];
                            //Console.WriteLine(context.Request.Cookies["good_cookie"]);
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();

            services.AddSingleton<IConfiguration>(builder.Configuration);
            services.AddTransient<UserService>();
            services.AddTransient<CardService>();

            builder.Services.AddDbContext<GuessItDbContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 42))
                )
            );

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = long.MaxValue;
            });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            var app = builder.Build();

            app.UseCors(builder => builder
            .SetIsOriginAllowed(origin => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());

            app.UseForwardedHeaders();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always
            });

            app.UseRouting();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                var path = context.Request.Path;

                if (path.StartsWithSegments("/giapi/swagger"))
                {
                    if (!context.User.Identity?.IsAuthenticated ?? true)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                    if (!context.User.IsInRole("admin"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                }
                await next();
            });


            app.UseSwagger(c =>
            {
                c.RouteTemplate = "giapi/swagger/{documentName}/swagger.json";
            });
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "giapi/swagger";
                options.SwaggerEndpoint("/giapi/swagger/v1/swagger.json", "v1");
            });

            app.MapControllers();

            app.Run();
        }
    }
}
