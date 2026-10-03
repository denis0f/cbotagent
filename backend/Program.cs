using backend.Data;
using backend.Services;
using backend.Tools;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<PromptRefinerAgentService>();
builder.Services.AddScoped<CoderAgentService>();
builder.Services.AddScoped<BotService>();
builder.Services.AddScoped<AgentRunService>();
builder.Services.AddScoped<BotCreationService>();
builder.Services.AddScoped<CompilationService>();
builder.Services.AddScoped<CompileSourceTool>();
builder.Services.AddScoped<CompilerAgentService>();
builder.Services.AddScoped<SaveBotVersionTool>();
builder.Services.AddScoped<CompileBotVersionTool>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();


