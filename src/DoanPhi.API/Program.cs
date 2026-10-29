using System.Text;
using DoanPhi.API.Middleware;
using DoanPhi.Application.Common.Interfaces;
using DoanPhi.Application.Features.Auth.Commands;
using DoanPhi.Application.Features.Submissions.Commands;
using DoanPhi.Application.Features.Submissions.Queries;
using DoanPhi.Application.Features.System;
using DoanPhi.Domain.Interfaces;
using DoanPhi.Infrastructure.Auth;
using DoanPhi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers
builder.Services.AddControllers();

// 2. Authentication & Authorization (JWT Bearer cho Role Admin)
var jwtKey = Encoding.UTF8.GetBytes(JwtTokenGenerator.SecretKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        ValidateIssuer = true,
        ValidIssuer = JwtTokenGenerator.Issuer,
        ValidateAudience = true,
        ValidAudience = JwtTokenGenerator.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

// 3. Swagger / OpenAPI Documentation có hỗ trợ Authorize Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "EduHub - Hệ Thống Thu Nộp Đoàn Phí & Hội Phí",
        Version = "v1",
        Description = "REST API thu thập thông tin, tự động hóa VietQR và đối soát nộp đoàn phí sinh viên theo Clean Architecture 4 lớp .NET 10"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token JWT theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 4. Clean Architecture Dependency Injection
// Cấu hình kết nối Supabase Cloud Database & Storage
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.SectionName));

// Repositories (Infrastructure -> Domain) đồng bộ Supabase Cloud
builder.Services.AddHttpClient<IFeeSubmissionRepository, SupabaseFeeSubmissionRepository>();
builder.Services.AddHttpClient<ISystemSettingRepository, SupabaseSystemSettingRepository>();
builder.Services.AddHttpClient<IAdminUserRepository, SupabaseAdminUserRepository>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// Command & Query Handlers (Application)
builder.Services.AddScoped<SubmitFeeCommandHandler>();
builder.Services.AddScoped<DeleteSubmissionCommandHandler>();
builder.Services.AddScoped<ClearAllSubmissionsCommandHandler>();
builder.Services.AddScoped<GetSubmissionsQueryHandler>();
builder.Services.AddScoped<GetSubmissionByIdQueryHandler>();
builder.Services.AddScoped<GetFeeStatsQueryHandler>();
builder.Services.AddScoped<ExportCsvQueryHandler>();

// Auth & System Handlers
builder.Services.AddScoped<LoginCommandHandler>();
builder.Services.AddScoped<ChangePasswordCommandHandler>();
builder.Services.AddScoped<GetSystemStatusQueryHandler>();
builder.Services.AddScoped<ToggleFormStatusCommandHandler>();

// 5. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 6. HTTP Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduHub Đoàn Phí API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");

// Cho phép phục vụ giao diện tĩnh từ wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
