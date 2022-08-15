using ChartJSCore.Helpers;
using ChartJSCore.Models;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using UselessYoutubeDataExtractor.Data;
using UselessYoutubeDataExtractor.Entities;
using UselessYoutubeDataExtractor.Models;
using UselessYoutubeDataExtractor.Services;

namespace UselessYoutubeDataExtractor.Pages
{
    public class ResultsModel : PageModel
    {
		private readonly IRNGService _rngService;
		private readonly IConfiguration _configuration;
		private readonly UselessYoutubeDataExtractorDbContext _context;

		public ResultsModel(IRNGService rngService, IConfiguration configuration, UselessYoutubeDataExtractorDbContext context)
		{
			_rngService = rngService;
			_configuration = configuration;
			_context = context;
		}

        [BindProperty(SupportsGet = true)]
		public string VideoId { get; set; }

		public float TitleLengthRatioToAverage { get; set; }
		public bool IsTitleLongerThanAverage { get; set; }

		public float LikeToDislikeRatio{ get; set; }
		public bool MorePeopleLikedTheVideo { get; set; }
		public float PercentageLikes { get; set; }
		public float PercentageDislikes { get; set; }

		public Chart CommentLengthLikesChart { get; set; }
		public Chart MostCommonWordsChart { get; set; }
		public Chart MostLikedWordsChart { get; set; }
		public Chart MostRepliedWordsChart { get; set; }

		public List<string> StatisticalCommentPossibleContent { get; set; }

		public VideoData VideoData { get; set; }
		public List<CommonWord> MostCommonWords { get; set; }
		public float AverageWordCount { get; set; }
		public List<RandomlyGeneratedComment> RandomlyGeneratedComments { get; set; }
        public List<LikedWord> MostLikedWords { get; set; }
		public List<MostRepliedWord> WordsWithMostReplies { get; set; }
		public float AverageLikesPerComment { get; set; }
		public float AverageRepliesPerComment { get; set; }
		public float CommentLengthToLikesRatio { get; set; }

		public async Task<IActionResult> OnGet()
        {
			TitleLengthRatioToAverage = 1.5f;
			IsTitleLongerThanAverage = true;

            var videoData = _context.VideoData
                .Include(x => x.Comments)
                .SingleOrDefault(x => x.Id == VideoId);

            if(videoData == null)
			{
                await ExtractNewVideoData();
			}
            else
			{
                await ExtractExistingVideoData(videoData);
            }

            return Page();
        }

        private async Task ExtractNewVideoData()
		{
			VideoData = new VideoData
			{
				Id = VideoId,
			};

			var youtubeService = new YouTubeService(new BaseClientService.Initializer()
			{
				ApiKey = _configuration["YoutubeApiKey"],
				ApplicationName = "UselessYoutubeDataExtractor",
			});

			var videoListRequest = youtubeService.Videos.List("snippet, statistics");
			videoListRequest.Id = VideoId;

			var videoListResponse = await videoListRequest.ExecuteAsync();
			var video = videoListResponse.Items.FirstOrDefault();
			VideoData.Title = video.Snippet.Title;
			VideoData.ThumbnailUrl = video.Snippet.Thumbnails.Maxres.Url;
			VideoData.LikeCount = (int)video.Statistics.LikeCount;
			//VideoData.DislikeCount = (int)video.Statistics.DislikeCount;

			var commentThreadsListRequest = youtubeService.CommentThreads.List("snippet");
			commentThreadsListRequest.VideoId = VideoId;
			commentThreadsListRequest.MaxResults = 100;
			commentThreadsListRequest.TextFormat = CommentThreadsResource.ListRequest.TextFormatEnum.PlainText;

			CommentThreadListResponse commentThreadListResponse;
			do
			{
				commentThreadListResponse = await commentThreadsListRequest.ExecuteAsync();
				foreach (var commentThread in commentThreadListResponse.Items)
				{
					var comment = new Entities.Comment(commentThread.Snippet.TopLevelComment.Snippet.TextDisplay, (int)commentThread.Snippet.TopLevelComment.Snippet.LikeCount, (int)commentThread.Snippet.TotalReplyCount);
					VideoData.Comments.Add(comment);
				}

				commentThreadsListRequest.PageToken = commentThreadListResponse.NextPageToken;
			}
			while (!string.IsNullOrEmpty(commentThreadListResponse.NextPageToken));

			_context.VideoData.Add(VideoData);
			await _context.SaveChangesAsync();

			GenerateData();
		}

		private async Task ExtractExistingVideoData(VideoData videoData)
		{
			VideoData = videoData;

			GenerateData();
		}

		private void GenerateData()
		{
			var totalLikes = VideoData.Comments.Sum(x => x.Likes);
			AverageLikesPerComment = (float)totalLikes / VideoData.Comments.Count;
			var totalReplies = VideoData.Comments.Sum(x => x.RepliesCount);
			AverageRepliesPerComment = (float)totalReplies / VideoData.Comments.Count;

			Dictionary<string, int> wordOccurences = new Dictionary<string, int>();
			Dictionary<string, int> wordLikes = new Dictionary<string, int>();
			Dictionary<string, int> wordReplies = new Dictionary<string, int>();

			int totalWordCount = 0;

			char[] delimiterChars = { ' ', ',', '.', '?', '!', ':', '\t' };
			foreach (var comment in VideoData.Comments)
			{
				var words = comment.Content.ToLower().Split(delimiterChars);
				totalWordCount += words.Length;
				foreach (var word in words)
				{
					if (string.IsNullOrEmpty(word))
					{
						continue;
					}

					if (wordOccurences.ContainsKey(word))
					{
						wordOccurences[word]++;
					}
					else
					{
						wordOccurences[word] = 1;

						if (wordLikes.ContainsKey(word))
						{
							wordLikes[word] += comment.Likes;
						}
						else
						{
							wordLikes[word] = comment.Likes;
						}

						if (wordReplies.ContainsKey(word))
						{
							wordReplies[word] += comment.RepliesCount;
						}
						else
						{
							wordReplies[word] = comment.RepliesCount;
						}
					}
				}
			}

			int count = 15;

			MostCommonWords = wordOccurences
				.OrderByDescending(x => x.Value)
				.Take(count)
				.Select(x => new CommonWord(x.Key, x.Value))
				.ToList();

			MostLikedWords = wordLikes
				.OrderByDescending(x => x.Value)
				.Take(count)
				.Select(x => new LikedWord(x.Key, x.Value))
				.ToList();

			WordsWithMostReplies = wordReplies
				.OrderByDescending(x => x.Value)
				.Take(count)
				.Select(x => new MostRepliedWord(x.Key, x.Value))
				.ToList();

			AverageWordCount = (float)totalWordCount / VideoData.Comments.Count;

			CommentLengthToLikesRatio = AverageWordCount / AverageLikesPerComment;

			MorePeopleLikedTheVideo = VideoData.LikeCount >= VideoData.DislikeCount;
			LikeToDislikeRatio = MorePeopleLikedTheVideo ? (float)VideoData.LikeCount / VideoData.DislikeCount : (float)VideoData.DislikeCount / VideoData.LikeCount;

			var totalVotes = VideoData.LikeCount + VideoData.DislikeCount;

			PercentageLikes = (float)VideoData.LikeCount / totalVotes;
			PercentageDislikes = (float)VideoData.DislikeCount / totalVotes;

			GenerateRandomComments(wordLikes, wordReplies);

			var commentLengthLikesData = GenerateCommentLengthLikesData();
			CommentLengthLikesChart = GenerateCommentLengthLikesChart(commentLengthLikesData);

			var mostCommonWordsData = GenerateMostCommonWordsData();
			MostCommonWordsChart = GenerateMostCommonWordsChart(mostCommonWordsData);

			var mostLikedWordsData = GenerateMostLikedWordsData();
			MostLikedWordsChart = GenerateMostLikedWordsChart(mostLikedWordsData);

			var mostRepliedWordsData = GenerateMostRepliedWordsData();
			MostRepliedWordsChart = GenerateMostRepliedWordsChart(mostRepliedWordsData);
		}

		private List<KeyValuePair<string, int>> GenerateMostCommonWordsData()
		{
			Dictionary<string, int> wordOccurences = new Dictionary<string, int>();

			char[] delimiterChars = { ' ', ',', '.', '?', '!', ':', '\t' };
			foreach (var comment in VideoData.Comments)
			{
				var words = comment.Content.ToLower().Split(delimiterChars);
				foreach (var word in words)
				{
					if(string.IsNullOrEmpty(word))
					{
						continue;
					}

					if (wordOccurences.ContainsKey(word))
					{
						wordOccurences[word]++;
					}
					else
					{
						wordOccurences[word] = 1;
					}
				}
			}

			int count = 20;
			var result = wordOccurences
				.OrderByDescending(x => x.Value)
				.Take(count)
				.ToList();
			return result;
		}

		private Chart GenerateMostCommonWordsChart(List<KeyValuePair<string, int>> plottedData)
		{
			Chart chart = new Chart();
			chart.Type = Enums.ChartType.Pie;

			ChartJSCore.Models.Data data = new ChartJSCore.Models.Data();
			data.Labels = plottedData.Select(x => x.Key).ToList();

			PieDataset dataset = new PieDataset();
			dataset.Label = "Most commonly used words";
			dataset.Data = plottedData.Select(x => (double?)x.Value).ToList();
			int minColorValue = 25;
			int maxColorValue = 255;
			dataset.BackgroundColor = plottedData.Select(x => {
				byte r = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				byte g = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				byte b = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				return ChartColor.FromRgb(r, g, b);
			}).ToList();
			dataset.HoverBackgroundColor = dataset.BackgroundColor.Select(x => ChartColor.FromRgba(x.Red, x.Green, x.Blue, 0.5)).ToList();

			data.Datasets = new List<Dataset>();
			data.Datasets.Add(dataset);

			chart.Data = data;

			return chart;
		}

		private List<KeyValuePair<string, int>> GenerateMostLikedWordsData()
		{
			Dictionary<string, int> wordLikes = new Dictionary<string, int>();

			char[] delimiterChars = { ' ', ',', '.', '?', '!', ':', '\t' };
			foreach (var comment in VideoData.Comments)
			{
				var words = comment.Content.ToLower().Split(delimiterChars);
				foreach (var word in words)
				{
					if (string.IsNullOrEmpty(word))
					{
						continue;
					}

					if (wordLikes.ContainsKey(word))
					{
						wordLikes[word] += comment.Likes;
					}
					else
					{
						wordLikes[word] = comment.Likes;
					}
				}
			}

			int count = 20;
			var result = wordLikes
				.OrderByDescending(x => x.Value)
				.Take(count)
				.ToList();
			return result;
		}

		private Chart GenerateMostLikedWordsChart(List<KeyValuePair<string, int>> plottedData)
		{
			Chart chart = new Chart();
			chart.Type = Enums.ChartType.Pie;

			ChartJSCore.Models.Data data = new ChartJSCore.Models.Data();
			data.Labels = plottedData.Select(x => x.Key).ToList();

			PieDataset dataset = new PieDataset();
			dataset.Label = "Most commonly used words";
			dataset.Data = plottedData.Select(x => (double?)x.Value).ToList();
			int minColorValue = 25;
			int maxColorValue = 255;
			dataset.BackgroundColor = plottedData.Select(x => {
				byte r = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				byte g = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				byte b = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				return ChartColor.FromRgb(r, g, b);
			}).ToList();
			dataset.HoverBackgroundColor = dataset.BackgroundColor.Select(x => ChartColor.FromRgba(x.Red, x.Green, x.Blue, 0.5)).ToList();

			data.Datasets = new List<Dataset>();
			data.Datasets.Add(dataset);

			chart.Data = data;

			return chart;
		}

		private List<KeyValuePair<string, int>> GenerateMostRepliedWordsData()
		{
			Dictionary<string, int> wordReplies = new Dictionary<string, int>();

			char[] delimiterChars = { ' ', ',', '.', '?', '!', ':', '\t' };
			foreach (var comment in VideoData.Comments)
			{
				var words = comment.Content.ToLower().Split(delimiterChars);
				foreach (var word in words)
				{
					if (string.IsNullOrEmpty(word))
					{
						continue;
					}

					if (wordReplies.ContainsKey(word))
					{
						wordReplies[word] += comment.RepliesCount;
					}
					else
					{
						wordReplies[word] = comment.RepliesCount;
					}
				}
			}

			int count = 20;
			var result = wordReplies
				.OrderByDescending(x => x.Value)
				.Take(count)
				.ToList();
			return result;
		}

		private Chart GenerateMostRepliedWordsChart(List<KeyValuePair<string, int>> plottedData)
		{
			Chart chart = new Chart();
			chart.Type = Enums.ChartType.Pie;

			ChartJSCore.Models.Data data = new ChartJSCore.Models.Data();
			data.Labels = plottedData.Select(x => x.Key).ToList();

			PieDataset dataset = new PieDataset();
			dataset.Label = "Most commonly used words";
			dataset.Data = plottedData.Select(x => (double?)x.Value).ToList();
			int minColorValue = 25;
			int maxColorValue = 255;
			dataset.BackgroundColor = plottedData.Select(x => {
				byte r = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				byte g = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				byte b = (byte)_rngService.Random.Next(minColorValue, maxColorValue);
				return ChartColor.FromRgb(r, g, b);
			}).ToList();
			dataset.HoverBackgroundColor = dataset.BackgroundColor.Select(x => ChartColor.FromRgba(x.Red, x.Green, x.Blue, 0.5)).ToList();

			data.Datasets = new List<Dataset>();
			data.Datasets.Add(dataset);

			chart.Data = data;

			return chart;
		}

		private List<KeyValuePair<int, int>> GenerateCommentLengthLikesData()
		{
			Dictionary<int, int> commentLengthTotalLikes = new Dictionary<int, int>();
			Dictionary<int, int> sameCommentLengthOccurences = new Dictionary<int, int>();

			char[] delimiterChars = { ' ', ',', '.', '?', '!', ':', '\t' };
			foreach (var comment in VideoData.Comments)
			{
				var words = comment.Content.ToLower().Split(delimiterChars);
				var wordCount = words.Length;

				if (commentLengthTotalLikes.ContainsKey(wordCount))
				{
					commentLengthTotalLikes[wordCount] += comment.Likes;
				}
				else
				{
					commentLengthTotalLikes[wordCount] = comment.Likes;
				}

				if (sameCommentLengthOccurences.ContainsKey(wordCount))
				{
					sameCommentLengthOccurences[wordCount]++;
				}
				else
				{
					sameCommentLengthOccurences[wordCount] = 1;
				}
			}

			Dictionary<int, int> averageCommentLengthLikes = new Dictionary<int, int>();

			foreach (var pair in commentLengthTotalLikes)
			{
				averageCommentLengthLikes[pair.Key] = pair.Value / sameCommentLengthOccurences[pair.Key];
			}

			return averageCommentLengthLikes.OrderBy(x => x.Key).ToList();
		}

		private Chart GenerateCommentLengthLikesChart(List<KeyValuePair<int, int>> plottedData)
		{
			Chart chart = new Chart();
			chart.Type = Enums.ChartType.Bar;

			ChartJSCore.Models.Data data = new ChartJSCore.Models.Data();
			data.Labels = plottedData.Select(x => x.Key.ToString()).ToList();

			BarDataset dataset = new BarDataset();
			dataset.Label = "No. of likes";
			dataset.Data = plottedData.Select(x => (double?)x.Value).ToList();
			dataset.BackgroundColor = new List<ChartColor>() { ChartColor.CreateRandomChartColor(false) };
			dataset.BorderColor = new List<ChartColor>() { ChartColor.CreateRandomChartColor(false) };
			dataset.BorderWidth = new List<int>() { 1 };

			data.Datasets = new List<Dataset>();
			data.Datasets.Add(dataset);

			chart.Data = data;

			Options options = new Options();

			Scales scales = new Scales();
			scales.YAxes = new List<Scale>
			{
				new CartesianScale
				{
					Ticks = new CartesianLinearTick
					{
						BeginAtZero = true,
					},
					ScaleLabel = new ScaleLabel
					{
						LabelString = "No of likes",
						FontSize = 20,
					},
				}
			};
			scales.XAxes = new List<Scale>
			{
				//new BarScale
				//{
				//	BarPercentage = 0.5,
				//	BarThickness = 6,
				//	MaxBarThickness = 8,
				//	MinBarLength = 0,
				//	GridLines = new GridLine
				//	{
				//		OffsetGridLines = true,
				//		DrawTicks = true,
				//		TickMarkLength = 7,
				//	},
				//},
				new CartesianScale
				{
					Ticks = new CartesianLinearTick
					{
						AutoSkip = true,
						AutoSkipPadding = 10,
					},
				}
			};

			options.Scales = scales;
			options.Responsive = true;
			options.MaintainAspectRatio = true;

			Layout layout = new Layout
			{
				Padding = new Padding
				{
					PaddingObject = new PaddingObject
					{
						Left = 10,
						Right = 10
					}
				}
			};

			options.Layout = layout;

			chart.Options = options;

			return chart;
		}

		private void GenerateRandomComments(Dictionary<string, int> wordLikes, Dictionary<string, int> wordReplies)
		{
            int keySize = 2;
            int commentCount = 15;

            Dictionary<string, List<string>> corpus = new Dictionary<string, List<string>>();

			foreach (var comment in VideoData.Comments)
			{
                var words = comment.Content.Split();
                if(words.Length < keySize)
				{
                    continue;
				}

				for (int i = 0; i <= words.Length - keySize; i++)
				{
                    var key = words.Skip(i).Take(keySize).Aggregate(Join);
                    string value = "";
                    if(i + keySize < words.Length)
					{
                        value = words[i + keySize];
					}

                    if(corpus.ContainsKey(key))
					{
                        corpus[key].Add(value);
					}
                    else
					{
                        corpus[key] = new List<string>() { value };
					}
				}
			}

			int statisticalCommentPossibilitiesCount = 10;
			StatisticalCommentPossibleContent = new List<string>(statisticalCommentPossibilitiesCount);
			for (int i = 0; i < statisticalCommentPossibilitiesCount; i++)
			{
				var content = RandomComment(corpus, keySize, (int)System.Math.Round(AverageWordCount));
				StatisticalCommentPossibleContent.Add(content);
			}

            RandomlyGeneratedComments = new List<RandomlyGeneratedComment>();
			for (int i = 0; i < commentCount; i++)
			{
                var commentLength = _rngService.Random.Next((int)AverageWordCount, (int)AverageWordCount + 25);
                var commentContent = RandomComment(corpus, keySize, commentLength);

				var comment = new RandomlyGeneratedComment();
				comment.Content = commentContent;

				// calculate like and reply count based on collected data about most liked words and most replied words
				char[] delimiterChars = { ' ', ',', '.', '?', '!', ':', '\t' };
				var words = commentContent.Split(delimiterChars);
				foreach (var word in words)
				{
					if(wordLikes.ContainsKey(word))
					{
						comment.LikeCount += wordLikes[word];
					}
					if (wordReplies.ContainsKey(word))
					{
						comment.ReplyCount += wordReplies[word];
					}
				}

				RandomlyGeneratedComments.Add(comment);
			}
			// normalize the like count values
			int totalLikeCount = RandomlyGeneratedComments.Sum(x => x.LikeCount);
			float averageLikeCount = totalLikeCount / commentCount;
			float likeDeviation = AverageLikesPerComment / averageLikeCount;
			int maxLikeCount = RandomlyGeneratedComments.Max(x => x.LikeCount);

			// normalize the reply count values
			int totalReplyCount = RandomlyGeneratedComments.Sum(x => x.LikeCount);
			float averageReplyCount = totalReplyCount / commentCount;
			float replyDeviation = AverageRepliesPerComment / averageReplyCount;
			int maxReplyCount = RandomlyGeneratedComments.Max(x => x.ReplyCount);

			foreach (var comment in RandomlyGeneratedComments)
			{
				comment.LikeCount = (int)(comment.LikeCount * likeDeviation) + (int)(averageLikeCount * (comment.LikeCount / maxLikeCount) / 10);
				comment.ReplyCount = (int)(comment.ReplyCount * comment.ReplyCount * replyDeviation) + (int)(averageReplyCount * ((comment.ReplyCount + 1) / maxReplyCount) / 10);
			}
		}

        private string RandomComment(Dictionary<string, List<string>> corpus, int keySize, int commentLength)
		{
            List<string> output = new List<string>();
            int n = 0;
            int rn = _rngService.Random.Next(corpus.Count);
            string prefix = corpus.Keys.Skip(rn).Take(1).Single();
            output.AddRange(prefix.Split());

            while(true)
			{
                if(!corpus.ContainsKey(prefix))
				{
					rn = _rngService.Random.Next(corpus.Count - keySize);
					prefix = corpus.Keys.Skip(rn).Take(1).Single();
				}

                var suffix = corpus[prefix];
                if(suffix.Count == 1)
				{
                    if(suffix[0] == "")
					{
						rn = _rngService.Random.Next(corpus.Count - keySize);
						prefix = corpus.Keys.Skip(rn).Take(1).Single();
						continue;
					}
                    output.Add(suffix[0]);
				}
                else
				{
                    rn = _rngService.Random.Next(suffix.Count);
                    output.Add(suffix[rn]);
				}

                if(output.Count >= commentLength)
				{
                    return output.Take(commentLength).Aggregate(Join);
				}
                n++;
                prefix = output.Skip(n).Take(keySize).Aggregate(Join);
			}
		}

        private string Join(string a, string b)
        {
            return a + " " + b;
        }
    }
}
