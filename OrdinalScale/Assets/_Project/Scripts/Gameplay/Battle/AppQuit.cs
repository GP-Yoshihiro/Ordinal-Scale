using UnityEngine;

namespace OrdinalScale.Gameplay.Battle
{
    /// <summary>
    /// 「終了」の処理（E4・Q-5）。実機（Quest の APK）では Application.Quit でアプリを閉じる。
    /// Editor ではアプリを閉じず（再生も止めず）、終了要求をログで確認する。
    /// Quest で Application.Quit が実際にアプリを閉じるかは未確認（実機で確認する）。
    /// </summary>
    public static class AppQuit
    {
        public static void Request(Object context)
        {
#if UNITY_EDITOR
            Debug.Log("[OrdinalScale][Q2] 終了要求を受け付けました（Editor ではアプリを閉じない。実機では Application.Quit を呼ぶ）", context);
#else
            Debug.Log("[OrdinalScale][Q2] 終了要求：Application.Quit を呼びます", context);
            Application.Quit();
#endif
        }
    }
}
