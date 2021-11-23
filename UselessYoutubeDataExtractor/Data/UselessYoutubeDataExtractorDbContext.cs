using Microsoft.EntityFrameworkCore;
using UselessYoutubeDataExtractor.Entities;

namespace UselessYoutubeDataExtractor.Data
{
	public class UselessYoutubeDataExtractorDbContext : DbContext
	{
		public UselessYoutubeDataExtractorDbContext(DbContextOptions<UselessYoutubeDataExtractorDbContext> options) : base(options)
		{
		}

		public DbSet<VideoData> VideoData { get; set; }
		public DbSet<Comment> Comments { get; set; }
	}
}
