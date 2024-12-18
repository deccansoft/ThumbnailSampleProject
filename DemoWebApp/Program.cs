﻿using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DemoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DemoDbContext")));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<DemoDbContext>();
context.Database.Migrate();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Employees}/{action=Index}/{id?}");

app.Run();
