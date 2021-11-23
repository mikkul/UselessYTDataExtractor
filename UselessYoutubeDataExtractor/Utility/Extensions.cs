namespace UselessYoutubeDataExtractor.Utility
{
	public static class Extensions
	{
		public static string ToPercentageString(this float value)
		{
			var percent = value * 100;
			return $"{percent}%";
		}
	}
}
