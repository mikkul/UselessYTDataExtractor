using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UselessYoutubeDataExtractor.Data;
using UselessYoutubeDataExtractor.Services;

namespace UselessYoutubeDataExtractor
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; set; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContext<UselessYoutubeDataExtractorDbContext>(options =>
			{
				options.UseMySql(Configuration.GetConnectionString("UselessYoutubeDataExtractorDatabase"), ServerVersion.AutoDetect(Configuration.GetConnectionString("UselessYoutubeDataExtractorDatabase")));
			});

			services.AddControllers();
			services.AddRazorPages();

			services.AddSingleton<IRNGService, RNGService>();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseStaticFiles();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
				endpoints.MapRazorPages();
			});
		}
	}
}
