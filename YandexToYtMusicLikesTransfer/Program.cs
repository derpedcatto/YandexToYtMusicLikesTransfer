using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using System;
using System.Text;

namespace YandexToYtMusicLikesTransfer
{
    internal class Program
    {


        static async Task Main(string[] args)
        {
            string CLIENT_ID = "";
            string CLIENT_SECRET = "";

            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("Reading tracklist...");
            var trackListTxtFilePath = @"D:\tracks.txt";
            var trackList = new List<string>(File.ReadAllLines(trackListTxtFilePath));
            if (trackList.Count <= 0) 
            {
                return;
            }
            Console.WriteLine("Reading complete! Total track count: " + trackList.Count);

            Console.WriteLine("Authorizing to Google...");

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = CLIENT_ID,
                    ClientSecret = CLIENT_SECRET
                },
                new[] { YouTubeService.Scope.Youtube, YouTubeService.Scope.YoutubeForceSsl },
                "user",
                CancellationToken.None,
                new FileDataStore("Youtube.Auth.Store")
            );

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "YandexMusicTransfer"
            });

            // Get the authenticated user's channel info
            var channelsListRequest = youtubeService.Channels.List("snippet");
            channelsListRequest.Mine = true;  // Get the channel of the authenticated user
            var channelsListResponse = await channelsListRequest.ExecuteAsync();

            if (channelsListResponse.Items.Count > 0)
            {
                var channel = channelsListResponse.Items[0];
                Console.WriteLine($"Authenticated as: {channel.Snippet.Title} (Channel ID: {channel.Id})");
            }
            else
            {
                Console.WriteLine("No channel found for the authenticated user.");
                return;
            }

            Console.WriteLine("Starting like transfer...");

            var logFilePath = @"D:\like_transfer_log.txt";
            var logBuilder = new System.Text.StringBuilder();

            foreach (var track in trackList)
            {
                try
                {
                    Console.WriteLine($"\n\nProcessing track: {track}");

                    // Perform a search
                    var searchListRequest = youtubeService.Search.List("snippet");
                    searchListRequest.Q = track + "official audio";
                    searchListRequest.Type = "video";
                    searchListRequest.MaxResults = 1;
                    searchListRequest.VideoCategoryId = "10"; // Music category

                    var searchListResponse = await searchListRequest.ExecuteAsync();

                    // Find a video from a "Topic" channel
                    var bestVideo = searchListResponse.Items
                        .FirstOrDefault(v => v.Snippet.ChannelTitle.Contains("Topic"))
                        ?? searchListResponse.Items.FirstOrDefault();

                    if (bestVideo != null)
                    {
                        var videoId = bestVideo.Id.VideoId;
                        var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
                        await youtubeService.Videos.Rate(bestVideo.Id.VideoId, VideosResource.RateRequest.RatingEnum.Like).ExecuteAsync();
                        var logEntry = $"{bestVideo.Snippet.Title} - {bestVideo.Snippet.ChannelTitle} [{videoUrl}]";
                        Console.WriteLine("Liked video: " + logEntry);
                        logBuilder.AppendLine(logEntry);
                    }
                    else
                    {
                        var logEntry = "No video found.";
                        Console.WriteLine(logEntry);
                        logBuilder.AppendLine($"{track} - {logEntry}");
                    }
                }
                catch (GoogleApiException ex) when (ex.Error.Code == 403)
                {
                    Console.WriteLine("Quota exceeded or access denied. Exiting.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    break;
                }
            }

            File.WriteAllText(logFilePath, logBuilder.ToString());

            Console.WriteLine("\n\nProcess complete! Log saved to like_transfer_log.txt");
        }
    }
}

/*
 * Video rating
 * https://developers.google.com/youtube/v3/docs/videos/rate
 * 
 */

/*

*/

/*
        static async Task Main(string[] args)
        {
            ApiParams.TokenYandexMusic = TOKEN_YANDEX;

            var yandexService = new YandexMusicApi.Api.Track(ApiParams);

            try
            {
                var yandexTrackList = await yandexService.GetLikesTrack(USERID_YANDEX);
                Console.Write(yandexTrackList.ToString());
                File.WriteAllText(@"d:\yandexlist.json", yandexTrackList.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

 */