using CeoMemo.Data;
using CeoMemo.Models.Human;
using CeoMemo.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Generators;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<HumanDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));

builder.Services.AddDbContext<PayrollDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("MySqlConnection"),
                     new MySqlServerVersion(new Version(8, 0, 21))));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
            ValidAudience = builder.Configuration["JWT:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
        };
    });

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("HRManagerOnly", policy => policy.RequireRole("HRManager"));
    options.AddPolicy("PayrollManagerOnly", policy => policy.RequireRole("PayrollManager"));
    options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee"));
});
builder.Services.AddScoped<JwtService>();
var app = builder.Build();
//using (var scope = app.Services.CreateScope())
//{
//    var humanDbContext = scope.ServiceProvider.GetRequiredService<HumanDbContext>();
//    humanDbContext.Database.Migrate();

//    if (!humanDbContext.Users.Any())
//    {
//        humanDbContext.Users.AddRange(
//            new User
//            {
//                Username = "admin",
//                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
//                Role = "Admin",
//                CreatedAt = DateTime.Now,
//                UpdatedAt = DateTime.Now
//            },
//            new User
//            {
//                Username = "hrmanager",
//                PasswordHash = BCrypt.Net.BCrypt.HashPassword("hr123"),
//                Role = "HRManager",
//                CreatedAt = DateTime.Now,
//                UpdatedAt = DateTime.Now
//            },
//            new User
//            {
//                Username = "payrollmanager",
//                PasswordHash = BCrypt.Net.BCrypt.HashPassword("payroll123"),
//                Role = "PayrollManager",
//                CreatedAt = DateTime.Now,
//                UpdatedAt = DateTime.Now
//            },
//            new User
//            {
//                Username = "employee",
//                PasswordHash = BCrypt.Net.BCrypt.HashPassword("emp123"),
//                Role = "Employee",
//                CreatedAt = DateTime.Now,
//                UpdatedAt = DateTime.Now
//            }
//        );
//        humanDbContext.SaveChanges();
//    }
//}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
