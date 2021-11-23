using System;

namespace UselessYoutubeDataExtractor.Services
{
	public interface IRNGService
	{
		public Random Random { get; }
	}

	public class RNGService : IRNGService
	{
		public RNGService()
		{
			Random = new Random();
		}

		public Random Random { get; }
	}
}
