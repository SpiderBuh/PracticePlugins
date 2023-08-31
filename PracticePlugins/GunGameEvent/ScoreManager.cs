using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using UnityEngine.SocialPlatforms.Impl;

namespace PracticePlugins.GunGameEvent
{
    public class ScoreManager
    {
        [Serializable]
        public class PlayerScore
        {
            public string UserLogName { get; set; }
            /// <summary>
            /// Stores total points at [0] and individual round points after
            /// </summary>
            public int[] Score { get; set; } = { 0 };
        }

        public class ScoreStorage
        {
            private const string FilePath = "plrScores.xml"; 

            public static List<PlayerScore> GetScores()
            {
                if (File.Exists(FilePath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<PlayerScore>));
                    using (TextReader reader = new StreamReader(FilePath))                    
                        return (List<PlayerScore>)serializer.Deserialize(reader);                    
                }
                else                
                    return new List<PlayerScore>();                
            }

            public static void SaveData(List<PlayerScore> data)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<PlayerScore>));
                using (TextWriter writer = new StreamWriter(FilePath))                
                    serializer.Serialize(writer, data);                
            }

            public static void AddScore(string uID, int score)
            {
                List<PlayerScore> data = GetScores();

                foreach (PlayerScore entry in data)
                    if (entry.UserLogName == uID)
                    {
                        entry.Score = entry.Score.Append(score).ToArray();
                        entry.Score[0] += score;
                        return;
                    }          

                data.Add(new PlayerScore { UserLogName = uID, Score = new int[] { score, score } });       
                SaveData(data);
            }
            public static void AddScore(Dictionary<string, int> ScoreList)
            {
                List<PlayerScore> data = GetScores();
                foreach (var kvp in ScoreList)
                {
                    bool found = false;
                    foreach (PlayerScore entry in data)                    
                        if (entry.UserLogName == kvp.Key)
                        {
                            entry.Score = entry.Score.Append(kvp.Value).ToArray();
                            entry.Score[0] += kvp.Value;
                            found = true;
                            break;
                        }
                    
                    if (!found)
                        data.Add(new PlayerScore { UserLogName = kvp.Key, Score = new int[] { kvp.Value, kvp.Value } });
                }
                SaveData(data);
            }
        }

   
    }
}
