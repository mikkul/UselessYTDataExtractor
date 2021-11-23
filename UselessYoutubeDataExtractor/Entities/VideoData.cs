using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UselessYoutubeDataExtractor.Entities
{
	public class VideoData
	{
		[Key]
		public string Id { get; set; }

		public ICollection<Comment> Comments { get; set; } = new List<Comment>();
		public string Title { get; set; }
		public int LikeCount { get; set; }
		public int DislikeCount { get; set; }
		public string ThumbnailUrl { get; set; }
	}
}
