using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;

namespace HEC_SupplyBot
{
    class Program
    {
        private static Timer _fetchTimer;
        private static Timer _statusTimer;
        private static HttpClient _httpClient;
        private static readonly string ApiUrl = "https://api.thegraph.com/subgraphs/name/hectordao-hec/hector-dao";
        private static IList<string> _statuses = new List<string>();
        private static int _statusIndex;
        private static DiscordClient _client;
        private static double _currentSupply;

        private static readonly string _apiOptions =
            "{\"variables\":{},\"query\":\"{  protocolMetrics(first: 210, orderBy: timestamp, orderDirection: desc) { totalSupply }}\"}";
        
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static string CalcDifference(string sNum1, string sNum2)
        {
            var dblNum1 = Convert.ToDouble(sNum1, CultureInfo.InvariantCulture);
            var dblNum2 = Convert.ToDouble(sNum2, CultureInfo.InvariantCulture);
            var dblDiff = (dblNum2 - dblNum1) / dblNum2;
            var strDiff = dblDiff.ToString("P", CultureInfo.InvariantCulture);
            return dblDiff * 100 > 0 ? $"+{strDiff}" : strDiff;
        }
        
        static async Task MainAsync()
        {
            _client = new DiscordClient(new DiscordConfiguration
            {
                Token = "Token",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.Guilds,
            });
            await _client.ConnectAsync();
            
            _client.Ready += async(sender, args) =>
            {
                _httpClient = new HttpClient(); 
            
                _fetchTimer = new Timer(5000);
                _fetchTimer.AutoReset = true;
                _fetchTimer.Start();
                _fetchTimer.Elapsed += FetchTimerOnElapsed;

                _statusTimer = new Timer(15000);
                _statusTimer.AutoReset = true;
                _statusTimer.Start();
                _statusTimer.Elapsed += StatusTimerOnElapsed;
            };
            
           await Task.Delay(-1);
        }

        private static async void StatusTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            await _client.UpdateStatusAsync(new DiscordActivity(_statuses[_statusIndex], ActivityType.Watching));
            await _client.Guilds[937023641235902534].CurrentMember.ModifyAsync(model =>
            {
                model.Nickname = $"{_currentSupply/1000000000:0.00} Total Supply";
            });
            _statusIndex = (_statusIndex + 1) % _statuses.Count;
        }
        
        private static async void FetchTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var response =  _httpClient.PostAsync("https://api.thegraph.com/subgraphs/name/hectordao-hec/hector-dao", new StringContent(_apiOptions)).Result;
            var supplyHistory = JsonSerializer.Deserialize<HecApiResponse>(response.Content.ReadAsStringAsync().Result);
            if (supplyHistory == null) return;
            _currentSupply = double.Parse(supplyHistory.Data.ProtocolMetrics[0].TotalSupply);
            var tmpStatuses = new List<string>
            {
                $"Diff (last rebase): {CalcDifference(supplyHistory.Data.ProtocolMetrics[2].TotalSupply, supplyHistory.Data.ProtocolMetrics[0].TotalSupply)}",
                $"Diff (5 rebases): {CalcDifference(supplyHistory.Data.ProtocolMetrics[10].TotalSupply, supplyHistory.Data.ProtocolMetrics[0].TotalSupply)}",
                $"Diff (100 rebases): {CalcDifference(supplyHistory.Data.ProtocolMetrics[100].TotalSupply, supplyHistory.Data.ProtocolMetrics[0].TotalSupply)}"
            };

            _statuses = tmpStatuses;
        }
    }
}