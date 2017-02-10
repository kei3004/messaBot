using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Commands;
using RiotSharp;
using RiotSharp.StaticDataEndpoint;
using RiotSharp.CurrentGameEndpoint;
using RiotSharp.SummonerEndpoint;
using RiotSharp.StatsEndpoint;

namespace MessaBot
{
    class Program
    {
        static void Main(string[] args) => new Program().Run(args).GetAwaiter().GetResult();
        RiotApi api = RiotApi.GetInstance("RIOT_TOKEN_HERE");
        StaticRiotApi staticApi = StaticRiotApi.GetInstance("RIOT_TOKEN_HERE");
        DiscordClient client;
        List<ChampionStatic> champions;
        List<SummonerSpellStatic> spells;
        public async Task Run(string[] args)
        {
            client = new DiscordClient(new DiscordConfig()
            {
                Token = "DISCORD_BOT_TOKEN",
                TokenType = TokenType.Bot,
                DiscordBranch = Branch.Canary,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true,
                AutoReconnect = true
            });
            client.UseCommands(new CommandConfig()
            {
                Prefix = "$",
                SelfBot = false
            });
            CreateCommands(client);
            await client.Connect();
            champions = staticApi.GetChampions(Region.na, ChampionData.info).Champions.Values.ToList<ChampionStatic>();
            spells = staticApi.GetSummonerSpells(Region.eune, SummonerSpellData.basic).SummonerSpells.Values.ToList<SummonerSpellStatic>();
            Console.ReadLine();
        }
        string championByID(long id)
        {
            return champions.Where(c => c.Id == id).FirstOrDefault().Name;
        }
        string spellByID(long id)
        {

            return spells.Where(s => s.Id== id).FirstOrDefault().Name;
        }
        List<string> rangBySummoner(List<Participant> l, List<Summoner> sum )
        {
            List<string> rang = new List<string>();
            foreach (var item in l)
            {
                try
                {
                    var ret = sum.Find(s=> s.Name ==item.SummonerName).GetLeagues();
                    rang.Add(ret.FirstOrDefault().Tier + " " + ret.FirstOrDefault().Entries.FirstOrDefault().Division);
                }
                catch(RiotSharpException )
                {
                    rang.Add("Unranked");
                }
            }
            return rang;
        }
        public List<string> statsByParticipants(List<Participant> s,List<Summoner> sum)
        {
            List<string> l = new List<string>();
            int i = 0;
            foreach (var item in s)
            {
                try
                {
                    Console.WriteLine("Recuperation des ranked stats par son id pour "+sum[i].Name);
                    var stats = api.GetStatsRanked(Region.euw, sum.Find(c=>c.Name == item.SummonerName).Id);
                    Console.WriteLine(item.SummonerName+" =? " + sum[i].Name);
                    Console.WriteLine("Fin de recup des ranked stats par son id");
                    var x = stats.Find(c => c.ChampionId == item.ChampionId);
                    var n = x.Stats.TotalSessionsPlayed;
                    var k = (x.Stats.TotalChampionKills / n).ToString();
                    var d = (x.Stats.TotalDeathsPerSession / n).ToString();
                    var a = (x.Stats.TotalAssists / n).ToString();
                    var st =  k+ "/" + d+ "/" + a;
                    Console.WriteLine("Pour {0} games : "+st,n);
                    l.Add(st);
                }
                catch (RiotSharpException)
                {
                    l.Add("0/0/0");
                }
                catch(Exception)
                {
                    l.Add("0/0/0");
                }
                i++;
            }
            
            return l;
        }
        public  Summoner summonerByName(string name)
        {
            Summoner x = api.GetSummoner(Region.euw, name);
            return x;
        }
        void CreateCommands(DiscordClient _client)
        {
            _client.AddCommand("Game", async (e) =>
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                string reponse = "";
                string requete = e.Message.Content;
                string[] split = requete.Split(new string[] { "$Game" }, StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine("Recherche de " + split[0] + ".");
                try
                {
                    var summoner = api.GetSummoner(Region.euw, split[0]);
                    var match = api.GetCurrentGame(Platform.EUW1,summoner.Id);
                    var participants = match.Participants;
                    List<string> sumId = new List<string>();
                    List<Summoner> sum = new List<Summoner>();
                    foreach (var item in participants)
                    {
                        sumId.Add(item.SummonerName);
                    }
                    sum = api.GetSummoners(Region.euw, sumId);
                    Console.WriteLine("Début du formattage du KDA");
                    var lStats = statsByParticipants(participants,sum);
                    Console.WriteLine("Fin du formattage du KDA");
                    var bans = match.BannedChampions;
                    bans.Sort(delegate (BannedChampion x, BannedChampion y)
                    {
                        return x.TeamId.CompareTo(y.TeamId);
                    });
                    string[] ban =  { championByID(bans[0].ChampionId), championByID(bans[1].ChampionId), championByID(bans[2].ChampionId), championByID(bans[3].ChampionId), championByID(bans[4].ChampionId), championByID(bans[5].ChampionId) };

                    reponse = "```Red Team-- Bans[" + ban[0]+", " + ban[1] +", " + ban[2] + "]"+Environment.NewLine+ Environment.NewLine;
                    Console.WriteLine("Debut de recup du rang");
                    var l = rangBySummoner(participants,sum);
                    Console.WriteLine("Fin de récup du rang");
                    int i = 0;
                    IEnumerable<Tuple<string, string, string, string, string, string>> authors =
                    new[]
                    {
                        Tuple.Create(participants[0].SummonerName,l[0] , championByID(participants[0].ChampionId),lStats[i],spellByID(participants[0].SummonuerSpell1),spellByID(participants[0].SummonerSpell2)),
                        Tuple.Create(participants[1].SummonerName,l[1] , championByID(participants[1].ChampionId),lStats[i+1],spellByID(participants[1].SummonuerSpell1),spellByID(participants[1].SummonerSpell2)),
                        Tuple.Create(participants[2].SummonerName,l[2] , championByID(participants[2].ChampionId),lStats[i+2],spellByID(participants[2].SummonuerSpell1),spellByID(participants[2].SummonerSpell2)),
                        Tuple.Create(participants[3].SummonerName,l[3] , championByID(participants[3].ChampionId),lStats[i+3],spellByID(participants[3].SummonuerSpell1),spellByID(participants[3].SummonerSpell2)),
                        Tuple.Create(participants[4].SummonerName,l[4] , championByID(participants[4].ChampionId),lStats[i+4],spellByID(participants[4].SummonuerSpell1),spellByID(participants[4].SummonerSpell2))
                    };

                    reponse += authors.ToStringTable(
                    new[] { "Pseudo", "Rang", "Champion", "KDA","Spell 1","Spell 2" },
                    a => a.Item1, a => a.Item2, a => a.Item3, a => a.Item4, a => a.Item5, a => a.Item6) +Environment.NewLine+ Environment.NewLine;
                    reponse += "Blue Team-- Bans[" + ban[3] + ", " + ban[4] + ", " + ban[5] + "]"+Environment.NewLine+ Environment.NewLine;
                    i = 5;
                    authors =
                    new[]
                    {
                        Tuple.Create(participants[5].SummonerName,l[i+0] , championByID(participants[5].ChampionId),"0/0/0",spellByID(participants[5].SummonuerSpell1),spellByID(participants[5].SummonerSpell2)),
                        Tuple.Create(participants[6].SummonerName,l[i+1] , championByID(participants[6].ChampionId),"0/0/0",spellByID(participants[6].SummonuerSpell1),spellByID(participants[6].SummonerSpell2)),
                        Tuple.Create(participants[7].SummonerName,l[i+2] , championByID(participants[7].ChampionId),"0/0/0",spellByID(participants[7].SummonuerSpell1),spellByID(participants[7].SummonerSpell2)),
                        Tuple.Create(participants[8].SummonerName,l[i+3] , championByID(participants[8].ChampionId),"0/0/0",spellByID(participants[8].SummonuerSpell1),spellByID(participants[8].SummonerSpell2)),
                        Tuple.Create(participants[9].SummonerName,l[i+4] , championByID(participants[9].ChampionId),"0/0/0",spellByID(participants[9].SummonuerSpell1),spellByID(participants[9].SummonerSpell2))
                    };
                    reponse += authors.ToStringTable(
                    new[] { "Pseudo", "Rang", "Champion", "KDA", "Spell 1", "Spell 2" },
                    a => a.Item1, a => a.Item2, a => a.Item3, a => a.Item4, a => a.Item5, a => a.Item6) + Environment.NewLine+ "```";
                }
                
                catch (RiotSharpException ex)
                {
                    reponse = "Je trouve pas, désolé ptit frère ...";
                    Console.WriteLine(ex);
                }
                watch.Stop();
                Console.WriteLine("La recherche a pris : " +watch.ElapsedMilliseconds+" ms");
                await e.Message.Parent.SendMessage(reponse);
            });
        }
    }
}
