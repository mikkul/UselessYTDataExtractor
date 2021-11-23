namespace UselessYoutubeDataExtractor.Models
{
	public class LikedWord
	{
		public LikedWord(string content, int score)
		{
			Content = content;
			Score = score;
		}

		public string Content { get; set; }
		public int Score { get; set; }
	}
}