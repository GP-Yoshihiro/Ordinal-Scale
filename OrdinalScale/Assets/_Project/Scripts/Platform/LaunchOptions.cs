using UnityEngine;

namespace OrdinalScale.Platform
{
    /// <summary>
    /// 起動時に外から渡すオプション（Q5：実機検証の補助）。
    /// Android（Quest）は起動 Intent の真偽値 extra を読む：<c>adb shell am start -S -n &lt;activity&gt; --ez &lt;key&gt; true</c>。
    /// Editor・iOS では常に false（Editor ではコンポーネントの Inspector の値を使う）。
    /// アプリがすでに動いている状態で am start しても前の Intent のままなので、-S で起動し直す（tools/quest/quest_device.sh が行う）。
    /// </summary>
    public static class LaunchOptions
    {
        /// <summary>剣の測定モード（敵は動かず攻撃せず、命中しても HP を減らさない）。</summary>
        public const string SwordPractice = "os_sword_practice";

        public static bool GetBool(string key)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = activity?.Call<AndroidJavaObject>("getIntent"))
                {
                    var value = intent != null && intent.Call<bool>("getBooleanExtra", key, false);
                    // 実機で起動オプションが渡ったかをログで確かめられるようにする（tools/quest/quest_device.sh の集計に出る）
                    Debug.Log($"[OrdinalScale][Q5] 起動オプション {key}={(value ? "true" : "false")}（Intent {(intent != null ? "あり" : "なし")}）");
                    return value;
                }
            }
            catch (System.Exception e)
            {
                // 読めなくても通常の戦闘で動かす（測定モードは補助なので起動を止めない）
                Debug.LogWarning($"[OrdinalScale][Q5] 起動オプション {key} を読めないため false として扱います: {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }
    }
}
