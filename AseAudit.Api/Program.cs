using AseAudit.Api.Services;
using AseAudit.Api.Services.Ingest;
using AseAudit.Infrastructure.Data;
using AseAudit.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 註冊 EF Core DbContext — 使用 SQL Server (LocalDB) + Windows 驗證
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuditDb")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Audit ingest pipeline (Collector -> Api -> DB)
// DbContext 為 Scoped，下游 Repository / IngestService 必須同為 Scoped
builder.Services.AddRepositories();
builder.Services.AddSnapshotHandlers();
builder.Services.AddScoped<IAuditIngestService, DatabaseAuditIngestService>();
builder.Services.AddScoped<IdentityRepository>();

// 放寬 JSON 上傳大小上限，避免大型快照 (例如事件記錄) 被截斷
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 64 * 1024 * 1024; // 64 MB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024 * 1024; // 64 MB
});
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("AuditDb");
    return new SqlConnection(connStr);
});
var app = builder.Build();

// 啟動時自動套用尚未執行的 EF Core Migrations（新增資料表/欄位皆透過 migration 管理）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 不使用 HTTPS 轉址：AgentIngest endpoint (:5001) 為純 HTTP，
// Collector 以 http POST 上傳稽核資料，啟用 redirect 會導致 307 → https 連線失敗。
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
