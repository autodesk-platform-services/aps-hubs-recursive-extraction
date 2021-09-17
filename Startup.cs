using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.AspNetCore.Mvc;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Hangfire;
using fileInfoExtract.Hubs;

namespace fileInfoExtract
{
  public class Startup
  {
    //https://stackoverflow.com/questions/58340247/how-to-use-hangfire-in-net-core-with-mongodb

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddMvc(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0).AddNewtonsoftJson();

      //you will use some way to get your connection string
      var mongoConnection = Environment.GetEnvironmentVariable("MONGO_CONNECTOR");
      var migrationOptions = new MongoMigrationOptions
      {
        MigrationStrategy = new MigrateMongoMigrationStrategy(),
        BackupStrategy = new CollectionMongoBackupStrategy()
      };

      services.AddHangfire(config =>
      {
        config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
        config.UseSimpleAssemblyNameTypeSerializer();
        config.UseRecommendedSerializerSettings();
        config.UseMongoStorage(mongoConnection, Environment.GetEnvironmentVariable("HANGFIREDATABASE"), new MongoStorageOptions { MigrationOptions = migrationOptions });

      });
      services.AddHangfireServer();

      services.AddSignalR(o =>
      {
        o.EnableDetailedErrors = true;
        o.MaximumReceiveMessageSize = 10240; // bytes
      });
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
        endpoints.MapHub<ContentsHub>("/contentshub");
      });

      app.UseMvc();
    }

  }
}
