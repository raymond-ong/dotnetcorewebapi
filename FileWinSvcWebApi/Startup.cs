﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IsaePrmDwApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
        //services.AddCors();
            //https://stackoverflow.com/questions/56164407/cors-asp-net-core-webapi-missing-access-control-allow-origin-header
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed((host) => true)
                        .AllowAnyHeader());
            });
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // https://docs.microsoft.com/en-us/aspnet/core/security/cors
            // Requires request to include "Origin" header.
            app.UseCors(builder =>
                        builder.WithOrigins("http://localhost", // Explicit domain and standard port.
                                            "http://localhost:3000",
                                            "http://10.131.27.69:3000") // 3000 is the port of the React App!
                                            .AllowAnyHeader()
                                            .AllowAnyMethod()
                        ); // Explicit domain and port.

            app.UseCors("CorsPolicy");

            UseWebSockets(app);

            app.UseMvc();
        }

        private void UseWebSockets(IApplicationBuilder app)
        {
            app.UseWebSockets(new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            });

            app.UseMiddleware<WebSocketsMiddleware>();
        }
    }
}