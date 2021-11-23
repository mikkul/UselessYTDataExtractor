namespace UselessYoutubeDataExtractor.Models
{
	public class CommonWord
	{
		public CommonWord(string content, int noOfOccurences)
		{
			Content = content;
			NoOfOccurences = noOfOccurences;
		}

		public string Content { get; set; }
		public int NoOfOccurences { get; set; }
	}
}
