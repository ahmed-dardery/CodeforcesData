using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CodeforcesData
{
    class Program
    {
        private readonly static string info = "https://codeforces.com/api/user.info";
        private readonly static string status = "https://codeforces.com/api/user.status";
        private static readonly HttpClient client = new HttpClient();
        public struct UserInfo
        {
            public int maxrating;
            public int rating;
            public int countProblems;
            public int totalscore;
            public int easycount;
            public int hardcount;
            public UserInfo(int maxrating, int rating, int count, int totalscore, int easycount, int hardcount)
            {
                this.maxrating = maxrating;
                this.rating = rating;
                this.countProblems = count;
                this.totalscore = totalscore;
                this.easycount = easycount;
                this.hardcount = hardcount;
            }
        }
        public async static Task<JObject> QueryAPI(string url, string user, string tag = "handles")
        {
            JObject resp;
            while (true)
            {

                var content = new FormUrlEncodedContent(new Dictionary<string, string> { { tag, user } });
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
                resp = JObject.Parse(responseString);
                if (resp["status"].ToString() != "OK" && resp["comment"].ToString().StartsWith("Call limit exceeded"))
                {
                    System.Threading.Thread.Sleep(500);
                    continue;
                }
                else break;
            }
            return resp;
        }
        async static Task Main(string[] args)
        {
            const int PROBLEM_THRESHOLD = 1600;
            var extra = new List<UserInfo?>();
            string[] handles = System.IO.File.ReadAllLines(@"handles.txt");
            foreach (string _user in handles)
            {
                string user = _user.Trim();
                JObject resp1 = await QueryAPI(info, user, "handles");
                JObject resp2 = await QueryAPI(status, user, "handle");

                if (resp1["status"].ToString() != "OK")
                {
                    extra.Add(null);
                    continue;
                }

                JArray arr = (JArray)(resp1["result"]);
                int rating = arr.First?.Value<int>("rating") ?? 0;
                int maxrating = arr.First?.Value<int>("maxRating") ?? 0;
                JArray submissions = (JArray)(resp2["result"]);
                var ac = submissions.Where(x => x.Value<string>("verdict") == "OK");
                int count = ac.Count();
                int countHard = ac.Where(x => x["problem"].Value<int>("rating") >= PROBLEM_THRESHOLD).Count();
                int countEasy = ac.Where(x => x["problem"].Value<int>("rating") < PROBLEM_THRESHOLD).Count();
                int totalScore = ac.Aggregate(0, (x, y) => x + y["problem"].Value<int>("rating"));
                extra.Add(new UserInfo(maxrating, rating, count, totalScore, countEasy, countHard));

                Console.WriteLine(user);
            }

            List<string> ans = extra.ConvertAll(new Converter<UserInfo?, string>(convert));
            System.IO.File.WriteAllLines("output.txt", ans.ToArray());
        }
        public static string convert(UserInfo? cur)
        {
            if (cur == null) return "";
            //maxrating, rating, count, total, easy, hard
            else return string.Join(",", cur?.maxrating, cur?.rating, cur?.countProblems, cur?.totalscore, cur?.easycount, cur?.hardcount);
        }
    }
}
