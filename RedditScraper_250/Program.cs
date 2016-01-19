using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RedditScraper_250 {
    class Program {
        static void Main(string[] args) {

            var redditApi = new RedditSharp.Reddit();
            var dailyProgrammer = redditApi.GetSubreddit("dailyprogrammer");

            var blankList = new[] { DailyProgrammerPost.Blank, DailyProgrammerPost.Blank, DailyProgrammerPost.Blank, };

            var grid = @"Easy | Intermediate | Hard | Weekly/Bonus

---- -| --------------| ------| -------------
| []() | []() | []() | **-** |
";

            var postsQuery = dailyProgrammer.Posts
                //.Take(20) // Used for development to speed things up
                //.ToList()
                //.OrderBy(p => p.Created)
                .Select(p => new DailyProgrammerPost(p.Title, p.Url.AbsoluteUri));

            List<DailyProgrammerPost> allPosts = new List<DailyProgrammerPost>();

            foreach (var post in postsQuery) {
                if (post.IsChallenge) {

                    allPosts.Add(post);
                }
                else if (post.IsWeekly || post.IsBonus) {

                    var week = allPosts.LastOrDefault()?.WeekNumber;

                    allPosts.Add(new DailyProgrammerPost(post.Title, post.Url, week));
                }
            }

            foreach (var weekOfPosts in allPosts.GroupBy(p => p.WeekNumber)) {

                var orderedPosts = weekOfPosts
                    .Where(p => p.IsChallenge)
                    .OrderBy(p => p.ChallengeDifficulty)
                    .Concat(weekOfPosts.Where(p => !p.IsChallenge))
                    .ToList();

                if (orderedPosts.Count() < 3) {

                    orderedPosts = orderedPosts.Concat(blankList).Take(3).ToList();
                }
                string extraAppend = orderedPosts.Count == 4 ? " |" : " | **-** |";

                var line = "| " + string.Join(" | ", orderedPosts) + extraAppend;

                grid += line + Environment.NewLine;
            }

            File.WriteAllText("c:\\\\temp\\DailyProgrammerReditOutput.txt", grid);
            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }

    public class DailyProgrammerPost {

        public static DailyProgrammerPost Blank => new DailyProgrammerPost("", "");

        private static readonly Regex weekNumberRegex = new Regex(@"Challenge #(\d+)", RegexOptions.IgnoreCase);
        private static readonly Regex difficultyRegex = new Regex(@"\[(Easy|Intermediate|Hard|difficult)\]", RegexOptions.IgnoreCase);

        private static readonly Regex isWeeklyRegex = new Regex(@"\[weekly[^\]]*\]", RegexOptions.IgnoreCase);
        private static readonly Regex isBonusRegex = new Regex(@"\[bonus]", RegexOptions.IgnoreCase);

        private readonly int? weekNumber; // use for bonus/other

        public DailyProgrammerPost(string title, string url, int? weekNumber = null) {

            this.Title = title;
            this.Url = url;
            this.weekNumber = weekNumber;
        }

        public string Title { get; set; }
        public string Url { get; set; }

        public bool IsChallenge => weekNumberRegex.IsMatch(this.Title) && difficultyRegex.IsMatch(this.Title);
        public bool IsWeekly => isWeeklyRegex.IsMatch(this.Title);
        public bool IsBonus => isWeeklyRegex.IsMatch(this.Title);

        public int WeekNumber => this.weekNumber ?? int.Parse(weekNumberRegex.Match(this.Title).Groups[1].Value);

        private string challengeDifficulty => difficultyRegex.Match(this.Title).Value.ToLower().Replace("[", "").Replace("]", "").Replace("difficult", "hard");
        public ChallengeDifficulty ChallengeDifficulty => (ChallengeDifficulty)Enum.Parse(typeof(ChallengeDifficulty), this.challengeDifficulty, true);

        public string LineOutput => $"[{this.Title}] ({this.Url})";

        public override string ToString() => this.LineOutput;
    }

    public enum ChallengeDifficulty {
        Easy = 0,
        Intermediate = 1,
        Hard = 2,
        //difficult = 3, // just convert difficult to hard instead
        Other = 4,
    }
}
