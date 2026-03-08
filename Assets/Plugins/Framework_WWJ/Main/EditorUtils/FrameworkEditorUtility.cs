using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plugins.Framework_WWJ.Main.EditorUtils
{
    public static class FrameworkEditorUtility
    {
        /// <summary>
        /// 获取当前 Project 窗口选中的文件夹路径
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentSelectedPath()
        {
            var path = "Assets";
            foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                path = Path.GetDirectoryName(path);
                break;
            }
            return path;
        }

        /// <summary>
        /// 获取当前选中的资产所在目录或选中的目录
        /// </summary>
        public static string GetCurrentDirectory()
        {
            var path = "Assets";
            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!AssetDatabase.IsValidFolder(path))
                {
                    path = Path.GetDirectoryName(path);
                }
            }
            return path?.Replace("\\", "/");
        }
    }
}
