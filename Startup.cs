using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Hangfire.MemoryStorage;


public class Startup
{
  public Startup(IConfiguration configuration)
  {
    Configuration = configuration;
  }

  public IConfiguration Configuration { get; }
  //https://stackoverflow.com/questions/58340247/how-to-use-hangfire-in-net-core-with-mongodb

  // This method gets called by the runtime. Use this method to add services to the container.
  // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddHangfire(config =>
    {
      config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
      config.UseSimpleAssemblyNameTypeSerializer();
      config.UseRecommendedSerializerSettings();
      config.UseMemoryStorage();

    });
    services.AddHangfireServer();

    services.AddControllers();
    services.AddSignalR(o =>
    {
      o.EnableDetailedErrors = true;
      o.MaximumReceiveMessageSize = 10240; // bytes
    });

    var APSClientID = Configuration["APS_CLIENT_ID"];
    var APSClientSecret = Configuration["APS_CLIENT_SECRET"];
    var APSCallbackURL = Configuration["APS_CALLBACK_URL"];
    if (string.IsNullOrEmpty(APSClientID) || string.IsNullOrEmpty(APSClientSecret) || string.IsNullOrEmpty(APSCallbackURL))
    {
      throw new ApplicationException("Missing required environment variables APS_CLIENT_ID, APS_CLIENT_SECRET, or APS_CALLBACK_URL.");
    }
    services.AddSingleton<APSService>(new APSService(APSClientID, APSClientSecret, APSCallbackURL));
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public void Configure(IApplicationBuilder app, IBackgroundJobClient backgroundJobs, IWebHostEnvironment env)
  {
    if (env.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
    }

    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseHangfireDashboard();
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
      endpoints.MapControllers();
      endpoints.MapHub<ContentsHub>("/contentshub");
    });
  }
}