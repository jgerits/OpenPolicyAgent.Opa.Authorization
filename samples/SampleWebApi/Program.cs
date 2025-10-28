using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenPolicyAgent.Opa.Authorization;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add authentication with JWT (for demonstration)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // In a real app, use proper configuration and secret management
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("super-secret-key-for-demo-purposes-only-at-least-32-chars"))
        };
        
        // For demo: allow authentication from simple Bearer token with username
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers.Authorization.ToString();
                if (token.StartsWith("Bearer simple-"))
                {
                    var username = token.Replace("Bearer simple-", "");
                    var role = username.Contains("admin") ? "admin" : "user";
                    
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Role, role),
                        new Claim("user_id", username)
                    };
                    
                    var identity = new ClaimsIdentity(claims, "SimpleAuth");
                    context.Principal = new ClaimsPrincipal(identity);
                    context.Success();
                }
                return Task.CompletedTask;
            }
        };
    });

// Add OPA authorization
builder.Services.AddOpaAuthorization(options =>
{
    options.OpaUrl = builder.Configuration["OpaUrl"] ?? "http://localhost:8181";
    options.DefaultPolicyPath = "authz/allow";
});

// Add controllers
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

