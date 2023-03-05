using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using WebRegApiCore;
using static IdentityModel.ClaimComparer;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
                      policy =>
                      {
                          policy.WithOrigins("http://softinfovm.in",
                                              "http://wizclip.in",
                                              "http://wizapp.in",
                                              "http://localhost:5173", "http://localhost:4200"
                                              )
                                            .AllowAnyHeader()
                                            .AllowAnyMethod(); ;
                      });
});

// Add services to the container.

builder.Services.AddControllers().AddNewtonsoftJson(options=>
options.UseMemberCasing())
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.Converters.Add(new CustomJsonConverterForType());
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    builder.Configuration.Bind("JwtSettings", options);
    options.Events = AuthEventsHandler.Instance;

    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddMvc(options =>
{
    options.AllowEmptyInputInBodyModelBinding = true;
    foreach (var formatter in options.InputFormatters)
    {
        if (formatter.GetType() == typeof(SystemTextJsonInputFormatter))
            ((SystemTextJsonInputFormatter)formatter).SupportedMediaTypes.Add(
            Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse("text/plain"));
    }
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});


//builder.Services.AddMvcCore(options =>
// {
//     options.RequireHttpsPermanent = true; // does not affect api requests
//     options.RespectBrowserAcceptHeader = true; // false by default
//                                                //options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>();

// });

builder.Services.AddScoped<RedirectingAction>();
//builder.Services.AddScoped<ControllerFilterExample>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c=>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});
//    c =>
//{
//    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
//});


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment() || app.Environment.IsProduction())

    app.UseSwagger();
    app.UseSwaggerUI();


app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

//app.UseCors(MyAllowSpecificOrigins);

// DO not forget to uncomment above line and comment below line to make API fully secure Whenever our WebApp goes live (Sanjay:13-02-2023) 
app.UseCors(builder =>
{
    builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<myCustomMiddleware>();

app.MapControllers();

app.Run();
