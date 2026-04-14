using AseAudit.Api.Services;
using AseAudit.Infrastructure.Data;
using AseAudit.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 註冊 EF Core DbContext — 使用 SQL Server (LocalDB) + Windows 驗證
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuditDb")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Audit ingest pipeline (Collector -> Api -> DB)
// DbContext 為 Scoped，下游 Repository / IngestService 必須同為 Scoped
builder.Services.AddScoped<IIdentificationAmAccountRepository, IdentificationAmAccountRepository>();
builder.Services.AddScoped<IIdentificationAmRuleRepository, IdentificationAmRuleRepository>();
builder.Services.AddScoped<IAuditIngestService, DatabaseAuditIngestService>();

// 放寬 JSON 上傳大小上限，避免大型快照 (例如事件記錄) 被截斷
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 64 * 1024 * 1024; // 64 MB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024 * 1024; // 64 MB
});

var app = builder.Build();

// 啟動時自動確認資料庫與資料表是否存在，若不存在則依 Entity 定義自動建立
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    db.Database.EnsureCreated();
}

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
