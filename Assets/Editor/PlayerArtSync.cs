using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BouncingPlanet.Editor
{
    /// <summary>
    /// 美术在 Raw/Player、Raw/clothes 下更新素材后，自动复制到 Image 的运行时目录，
    /// 并统一文件名（修正 Face_down、Hand_Overdirve、colth 等历史命名）。
    /// 只覆盖图片文件，保留 Image 下已有的 .meta（GUID / 导入设置稳定）。
    /// </summary>
    public sealed class PlayerArtSync : AssetPostprocessor
    {
        private const string RawRoot = "Assets/Resources/Raw/";
        private const string ImageRoot = "Assets/Resources/Image/";

        private static readonly (string source, string target)[] Mapping =
        {
            ("Player/paper/Paper.PNG", "Player/Parts/Paper.png"),
            ("Player/body/Body_Normal.PNG", "Player/Parts/Body_Normal.png"),
            ("Player/body/Body_Down.PNG", "Player/Parts/Body_Down.png"),
            ("Player/body/Body_Overdrive.PNG", "Player/Parts/Body_Overdrive.png"),
            ("Player/face/Face_Happy.PNG", "Player/Parts/Face_Happy.png"),
            ("Player/face/Face_down.PNG", "Player/Parts/Face_Down.png"),
            ("Player/face/Face_Up.PNG", "Player/Parts/Face_Up.png"),
            ("Player/face/Face_Overdrive.PNG", "Player/Parts/Face_Overdrive.png"),
            ("Player/hand/Hand_Happy.PNG", "Player/Parts/Hand_Happy.png"),
            ("Player/hand/Hand_Down.PNG", "Player/Parts/Hand_Down.png"),
            ("Player/hand/Hand_Up.PNG", "Player/Parts/Hand_Up.png"),
            ("Player/hand/Hand_Overdirve.PNG", "Player/Parts/Hand_Overdrive.png"),
            ("Player/hair/Hair1.PNG", "Player/Parts/Hair_1.png"),
            ("Player/hair/Hair2.PNG", "Player/Parts/Hair_2.png"),
            ("Player/hair/Hair3.PNG", "Player/Parts/Hair_3.png"),
            ("Player/cloth/Cloth1.PNG", "Player/Parts/Cloth_1.png"),
            ("Player/cloth/Cloth2.PNG", "Player/Parts/Cloth_2.png"),
            ("Player/cloth/Cloth3.PNG", "Player/Parts/Cloth_3.png"),
            ("Player/decoration/Decoration1.PNG", "Player/Parts/Decoration_1.png"),
            ("Player/decoration/Decoration2.PNG", "Player/Parts/Decoration_2.png"),
            ("Player/decoration/Decoration3.PNG", "Player/Parts/Decoration_3.png"),
            ("clothes/hair/hair1.png", "Clothes/Hair_1.png"),
            ("clothes/hair/hair2.png", "Clothes/Hair_2.png"),
            ("clothes/hair/hair3.png", "Clothes/Hair_3.png"),
            ("clothes/colth/cloth1.png", "Clothes/Cloth_1.png"),
            ("clothes/colth/cloth2.png", "Clothes/Cloth_2.png"),
            ("clothes/colth/cloth3.png", "Clothes/Cloth_3.png"),
            ("clothes/decoration/Decoration1.png", "Clothes/Decoration_1.png"),
            ("clothes/decoration/Decoration2.png", "Clothes/Decoration_2.png"),
            ("clothes/decoration/Decoration3.png", "Clothes/Decoration_3.png"),
        };

        private static bool scheduled;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!TouchesRawFolder(importedAssets) &&
                !TouchesRawFolder(movedAssets))
            {
                return;
            }

            if (scheduled)
                return;
            scheduled = true;
            EditorApplication.delayCall += () =>
            {
                scheduled = false;
                SyncAll(false);
            };
        }

        [MenuItem("Tools/BouncingPlanet/Art/Sync Player Parts To Image")]
        public static void SyncFromMenu()
        {
            SyncAll(true);
        }

        private static bool TouchesRawFolder(string[] paths)
        {
            if (paths == null)
                return false;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path))
                    continue;
                if (path.StartsWith(RawRoot + "Player/",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(RawRoot + "clothes/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static void SyncAll(bool verbose)
        {
            int copied = 0;
            for (int i = 0; i < Mapping.Length; i++)
            {
                string source = RawRoot + Mapping[i].source;
                string target = ImageRoot + Mapping[i].target;
                if (!File.Exists(source))
                    continue;

                string targetDir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(targetDir))
                    Directory.CreateDirectory(targetDir);

                bool isNew = !File.Exists(target);
                File.Copy(source, target, true);
                if (isNew)
                    CopyMetaWithNewGuid(source + ".meta", target + ".meta");
                copied++;
            }

            if (copied > 0)
                AssetDatabase.Refresh();
            if (verbose)
            {
                Debug.Log("[PlayerArtSync] 同步完成，共 " + copied +
                          " 个文件。");
            }
        }

        private static void CopyMetaWithNewGuid(string sourceMeta, string targetMeta)
        {
            if (!File.Exists(sourceMeta) || File.Exists(targetMeta))
                return;

            string text = File.ReadAllText(sourceMeta);
            string guid = Guid.NewGuid().ToString("N");
            text = System.Text.RegularExpressions.Regex.Replace(
                text, "^guid: [0-9a-f]{32}", "guid: " + guid,
                System.Text.RegularExpressions.RegexOptions.Multiline);
            File.WriteAllText(targetMeta, text);
        }
    }
}
