namespace UselessYoutubeDataExtractor.Models
{
	public class MostRepliedWord
	{
		public MostRepliedWord(string content, int repliesCount)
		{
			Content = content;
			RepliesCount = repliesCount;
		}

		public string Content { get; set; }
		public int RepliesCount { get; set; }
	}
}