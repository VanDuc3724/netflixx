﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using Netflixx.Services;
using Netflixx.Services.Vnpay;
using Netflixx.Hubs;

namespace Netflixx
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Connect VNPay API
            builder.Services.AddScoped<IVnPayService, VnPayService>();
            // Đăng ký OtpService
            builder.Services.AddSingleton<IOtpService, OtpService>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

            builder.Services.AddSingleton<IOtpGenerator, SecureOtpGenerator>();


            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSignalR(); // <-- Thêm SignalR service ở đây


            //Add email sender
            builder.Services.AddTransient<IEmailSender, EmailSender>();
            builder.Services.AddScoped<DbContext>();
            builder.Services.AddScoped<INotificationService, NotificationService>(); // <-- Thêm NotificationService

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                });

            //Add session
            builder.Services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
            });



            // Thêm services vào container
            builder.Services.AddDbContext<DBContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Đăng ký Identity
            builder.Services.AddIdentity<AppUserModel, IdentityRole>()
                .AddEntityFrameworkStores<DBContext>()
                .AddDefaultTokenProviders();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.AllowedForNewUsers = false;
                options.Lockout.MaxFailedAccessAttempts = 100;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Zero;

                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 0;

                // User settings.
                options.User.RequireUniqueEmail = false;
            });

            builder.Services.AddHostedService<DailyQuizSelector>();

            //avatar service
            builder.Services.AddScoped<IAvatarService, AvatarService>();
            builder.Services.AddSingleton<IChatRepository, ChatRepository>();

            builder.Services.AddScoped<IChatService, ChatService>();
            //chat hub
            builder.Services.AddSignalR();

            builder.Services.AddSingleton<IFileStorageService, AzureBlobStorageService>();

            //chatbot
            builder.Services.AddHttpClient();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            

            app.MapControllerRoute(
                name: "contact",
                pattern: "contact",
                defaults: new { controller = "Contact", action = "Index" });

            app.MapControllerRoute(
                name: "feedback",
                pattern: "feedback",
                defaults: new { controller = "Feedback", action = "Index" });
            app.UseHttpsRedirection();
            app.UseStaticFiles();

          

            app.UseRouting();
            app.MapHub<ChatHub>("/hubs/chat");

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<NotificationHub>("/notificationHub"); // <-- Thêm SignalR Hub route ở đây
            app.MapControllers();

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Login}/{action=Login}/{id?}");
                

            app.Run();
        }
    }
}