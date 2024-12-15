

using Reporting.lib.Data.Services.Emails;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<IDataConnection, DataConnection>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IEmailService, EmailService>();


builder.Services.AddControllers();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
// Map controllers
app.MapControllers();
app.UseDeveloperExceptionPage();
app.Run();

