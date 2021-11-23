using System.ComponentModel.DataAnnotations;

namespace UselessYoutubeDataExtractor.Entities
{
	public class Comment
	{
		public Comment(string content, int likes, int repliesCount)
		{
			Content = content;
			Likes = likes;
			RepliesCount = repliesCount;
		}

		[Key]
		public int Id { get; set; }

		public string Content { get; set; }
		public int Likes { get; set; }
		public int RepliesCount { get; set; }

		public VideoData VideoData { get; set; }
	}
}
