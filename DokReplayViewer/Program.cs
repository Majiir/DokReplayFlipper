using LZ4;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace DokReplayFlipper
{
    class Program
    {
        static void Main(string[] args)
        {
            var filename = args[0];

            using (var fileStream = File.OpenRead(filename))
            using (var stream = new LZ4Stream(fileStream, LZ4StreamMode.Decompress))
            using (var reader = new StreamReader(stream))
            {
                var text = reader.ReadToEnd();

                dynamic replay = JsonConvert.DeserializeObject(text);

                var nodes = (JArray)replay;

                var header = nodes.First(t => t.Value<string>("__type") == "BBI.Game.Replay.ReplayHelpers+ReplayableGameSessionHeader");
                var frames = nodes.Where(t => t.Value<string>("__type") == "BBI.Game.Replay.ReplayableGameSession+FrameData");

                var players = header["SessionPlayers"].ToList();

                foreach (var player in players)
                {
                    header["LocalPlayerID"] = player["PlayerID"];
                    var origPlayerName = player["PlayerName"];

                    player["PlayerName"] = JToken.FromObject(origPlayerName.Value<string>() + " (H)");

                    var teams = players.GroupBy(p => p["TeamID"].Value<int>());
                    var playersString = string.Join(" vs ", teams.Select(t => string.Join(", ", t.Select(p => formatUsername(p["PlayerName"].Value<string>())))));

                    var newFilename = string.Format("{2:yyyy-MM-dd (HH-mm)} {0} - {1}.dokreplay", playersString, readableMapName(header["SceneName"].Value<string>()), header["SaveTime"].Value<DateTime>());

                    saveReplay(replay, Path.Combine(Path.GetDirectoryName(filename), newFilename));

                    player["PlayerName"] = origPlayerName;
                }

            }
        }

        private static void saveReplay(JArray replay, string filename)
        {
            var serialized = JsonConvert.SerializeObject(replay);

            using (var writeStream = File.OpenWrite(filename))
            using (var compress = new LZ4Stream(writeStream, LZ4StreamMode.Compress))
            using (var writer = new StreamWriter(compress))
            {
                writer.Write(serialized);
            }
        }

        private static string formatUsername(string username)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                username = username.Replace(invalid, '.');
            }

            return username;
        }

        private static string readableMapName(string sceneName)
        {
            if (sceneName == "MP_05-Teeth") { return "Kalash Teeth (2)"; }
            if (sceneName == "MP_01-Crater") { return "Torin Crater (2)"; }
            if (sceneName == "MP_14_1v1_Smaller") { return "The Boneyard (2)"; }
            if (sceneName == "MP_17_1v1_2") { return "The Shallows (2)"; }
            if (sceneName == "MP_16_1v1-Firebase") { return "Firebase Kriil (2)"; }
            if (sceneName == "MP_23") { return "Gaalsien Territories (2)"; }
            if (sceneName == "MP_21_1v1") { return "Taiidan Passage (2)"; }

            if (sceneName == "MP_16-Firebase") { return "Firebase Kriil (4)"; }
            if (sceneName == "MP_14") { return "The Boneyard (4)"; }
            if (sceneName == "MP_07-Output") { return "Canyon Outpost (4)"; }
            if (sceneName == "MP_11-DuneSea") { return "Dune Sea (4)"; }
            if (sceneName == "MP_17_2v2") { return "The Shallows (4)"; }
            if (sceneName == "MP_21") { return "Taiidan Passage (4)"; }
            if (sceneName == "MP_22_2v2") { return "Kalash Valley (4)"; }

            if (sceneName == "MP_10-KharToba") { return "Khar-Toba (6)"; }
            if (sceneName == "MP_22") { return "Kalash Valley (6)"; }

            return sceneName;
        }
    }
}
