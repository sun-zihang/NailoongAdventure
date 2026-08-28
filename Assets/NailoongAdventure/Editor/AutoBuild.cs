using System.IO;
using UnityEditor;

namespace Nailoong.EditorTools
{
    /// <summary>
    /// 自动构建：工程首次在编辑器中打开（且脚本编译通过后）自动执行一遍全量生成，
    /// 用户无需手动点击菜单。已生成过一次后不再重复执行，可用菜单「奶龙/一键生成 Demo」随时重建。
    /// </summary>
    [InitializeOnLoad]
    public static class AutoBuild
    {
        public const string DoneKey = "NailoongAdventure.AutoBuilt.v1";

        static AutoBuild()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorPrefs.GetBool(DoneKey, false)) return;

            string firstScene = GameBuilder.SCENES + "/Level1_Beach.unity";
            if (File.Exists(firstScene))
            {
                EditorPrefs.SetBool(DoneKey, true);
                return;
            }

            UnityEngine.Debug.Log("[奶龙] 检测到首次打开工程，自动开始生成 Demo…（若需手动重建，使用菜单「奶龙/一键生成 Demo」）");
            GameBuilder.BuildAll();   // 成功后写入 DoneKey；若失败，下次打开仍会自动重试
        }
    }
}
