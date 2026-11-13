using System;
using UnityEngine;

namespace GufaCoding
{
    /// <summary>本地存档（PlayerPrefs，微信小游戏里会落到本地存储）。</summary>
    public static class SaveData
    {
        const string P = "gufa_";

        static int GetI(string key, int def = 0) { return PlayerPrefs.GetInt(P + key, def); }
        static void SetI(string key, int v) { PlayerPrefs.SetInt(P + key, v); PlayerPrefs.Save(); }

        public static int Salary { get { return GetI("salary"); } set { SetI("salary", Mathf.Max(0, value)); } }
        public static int Coffee { get { return GetI("coffee"); } set { SetI("coffee", Mathf.Max(0, value)); } }
        public static bool HasDuck { get { return GetI("duck") == 1; } set { SetI("duck", value ? 1 : 0); } }
        public static int TotalHairLost { get { return GetI("hair_lost"); } set { SetI("hair_lost", value); } }
        public static int TotalBugs { get { return GetI("bugs"); } set { SetI("bugs", value); } }

        public static bool IsCleared(int level) { return GetI("cleared_" + level) == 1; }
        public static void SetCleared(int level) { SetI("cleared_" + level, 1); }

        // —— 激励视频每日次数 ——
        static string Today { get { return DateTime.Now.ToString("yyyyMMdd"); } }
        public static int GetDaily(string placement) { return GetI("ad_" + placement + "_" + Today); }
        public static void AddDaily(string placement) { SetI("ad_" + placement + "_" + Today, GetDaily(placement) + 1); }

        public static void ResetAll(int levelCount)
        {
            foreach (var k in new[] { "salary", "coffee", "duck", "hair_lost", "bugs" }) PlayerPrefs.DeleteKey(P + k);
            for (int i = 0; i < levelCount; i++) PlayerPrefs.DeleteKey(P + "cleared_" + i);
            PlayerPrefs.Save();
        }
    }
}
