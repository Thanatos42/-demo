using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Play 模式调整自动回写工具：
/// 退出 Play 时自动记录各界面整棵节点树的 RectTransform，回到编辑模式后把有变化的值回写。
/// 特例：美术库驱动的前台小人（Chibi 主角/顾客）不写场景，改写 MilkTeaArtLibrary 的对应字段，
/// 否则下次运行会被控制器用美术库数值覆盖。
/// 快照存放在项目根目录的 MilkTeaPlayModeSnapshot.json，方便事后手动应用。
/// </summary>
public static class MilkTeaPlayModeSync
{
    private enum SyncTarget
    {
        SceneRect = 0,   // 直接写回场景节点的 RectTransform
        ArtLibrary = 1,  // 写回 MilkTeaArtLibrary 资产的 Vector2 字段（运行时控制器会套用）
    }

    // 记录这些界面根节点下的全部节点（含隐藏节点）。想加界面就往这里添一行。
    private static readonly string[] SyncedSubtrees =
    {
        "Milk Tea Demo Canvas/16:9 Content/Dialogue Screen",
        "Milk Tea Demo Canvas/16:9 Content/Mixing Screen",
        "Milk Tea Demo Canvas/16:9 Content/Rest Screen",
        "Milk Tea Demo Canvas/16:9 Content/Settlement Screen",
        "Milk Tea Demo Canvas/16:9 Content/Start Screen",
        "Milk Tea Demo Canvas/16:9 Content/Intro Screen",
        "Milk Tea Demo Canvas/16:9 Content/Settings Panel",
    };

    // 美术库驱动的节点：位置尺寸每次运行由控制器套用，回写场景无意义，改写美术库字段。
    private static readonly SyncEntry[] ArtLibraryEntries =
    {
        new SyncEntry
        {
            path = "Milk Tea Demo Canvas/16:9 Content/Dialogue Screen/Chibi 主角",
            positionField = "protagonistChibiPosition",
            sizeField = "protagonistChibiSize",
        },
        new SyncEntry
        {
            path = "Milk Tea Demo Canvas/16:9 Content/Dialogue Screen/Chibi 顾客",
            positionField = "customerChibiPosition",
            sizeField = "customerChibiSize",
        },
    };

    private const float Epsilon = 0.0001f;
    private const string SnapshotFileName = "MilkTeaPlayModeSnapshot.json";

    private static MilkTeaArtLibrary cachedArtLibrary;

    private class SyncEntry
    {
        public string path;
        public string positionField;
        public string sizeField;
    }

    [Serializable]
    private class NodeSnapshot
    {
        public string path;
        public int target;
        public string positionField;
        public string sizeField;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
        public Vector3 localEuler;
    }

    [Serializable]
    private class SnapshotFile
    {
        public List<NodeSnapshot> items = new List<NodeSnapshot>();
    }

    [InitializeOnLoadMethod]
    private static void Install()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingPlayMode:
            {
                int captured = CaptureToFile();
                if (captured > 0)
                {
                    Debug.Log($"[Play 调整同步] 已存档 {captured} 个节点的布局快照（此步只是记录，不代表有改动；回写结果看下一条日志）。");
                }

                break;
            }

            case PlayModeStateChange.EnteredEditMode:
            {
                int applied = ApplySnapshot();
                if (applied > 0)
                {
                    Debug.Log($"[Play 调整同步] 已自动回写 {applied} 处调整（场景/美术库），记得 Ctrl+S 保存场景！");
                }

                break;
            }
        }
    }

    [MenuItem("奶茶店 Demo/Play 调整同步/手动记录快照（Play 中暂停后点）")]
    private static void CaptureManually()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Play 调整同步] 请在 Play 模式（建议先暂停）下再记录。");
            return;
        }

        int captured = CaptureToFile();
        Debug.Log($"[Play 调整同步] 已存档 {captured} 个节点的布局快照。");
    }

    [MenuItem("奶茶店 Demo/Play 调整同步/手动应用快照")]
    private static void ApplyManually()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[Play 调整同步] 请先退出 Play 模式再应用。");
            return;
        }

        int applied = ApplySnapshot();
        Debug.Log($"[Play 调整同步] 已回写 {applied} 处调整（场景/美术库），记得 Ctrl+S 保存场景！");
    }

    private static int CaptureToFile()
    {
        SnapshotFile data = new SnapshotFile();
        HashSet<string> artLibraryPaths = new HashSet<string>();

        // 美术库驱动节点：记录运行时套用的 anchoredPosition/sizeDelta
        foreach (SyncEntry entry in ArtLibraryEntries)
        {
            RectTransform rect = FindByPath(entry.path);
            if (rect == null)
            {
                Debug.LogWarning($"[Play 调整同步] 找不到节点，已跳过：{entry.path}");
                continue;
            }

            artLibraryPaths.Add(entry.path);
            data.items.Add(ToArtLibrarySnapshot(entry, rect));
        }

        // 各界面子树：全量记录 RectTransform
        foreach (string subtree in SyncedSubtrees)
        {
            Transform root = FindByPath(subtree);
            if (root == null)
            {
                Debug.LogWarning($"[Play 调整同步] 找不到界面根节点，已跳过：{subtree}");
                continue;
            }

            RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rect in rects)
            {
                string nodePath = GetPath(rect.transform);
                if (artLibraryPaths.Contains(nodePath))
                {
                    continue;
                }

                data.items.Add(ToSceneSnapshot(nodePath, rect));
            }
        }

        if (data.items.Count == 0)
        {
            return 0;
        }

        File.WriteAllText(SnapshotFilePath(), JsonUtility.ToJson(data, true));
        return data.items.Count;
    }

    private static int ApplySnapshot()
    {
        string file = SnapshotFilePath();
        if (!File.Exists(file))
        {
            return 0;
        }

        SnapshotFile data;
        try
        {
            data = JsonUtility.FromJson<SnapshotFile>(File.ReadAllText(file));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Play 调整同步] 快照文件解析失败，已忽略：{e.Message}");
            return 0;
        }

        if (data == null || data.items == null || data.items.Count == 0)
        {
            return 0;
        }

        Dictionary<string, RectTransform> sceneIndex = BuildSceneRectIndex();
        bool sceneDirty = false;
        bool artChanged = false;
        int applied = 0;
        foreach (NodeSnapshot snapshot in data.items)
        {
            if ((SyncTarget)snapshot.target == SyncTarget.ArtLibrary)
            {
                if (ApplyToArtLibrary(snapshot))
                {
                    artChanged = true;
                    applied++;
                }

                // 缩放折算进美术库宽高后，把场景节点缩放复位，防止下次运行双重放大。
                if (sceneIndex.TryGetValue(snapshot.path, out RectTransform artNode)
                    && Changed(artNode.localScale, Vector3.one))
                {
                    artNode.localScale = Vector3.one;
                    EditorUtility.SetDirty(artNode);
                    sceneDirty = true;
                }

                continue;
            }

            if (sceneIndex.TryGetValue(snapshot.path, out RectTransform rect) && ApplyToSceneRect(rect, snapshot))
            {
                sceneDirty = true;
                applied++;
            }
        }

        if (sceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        if (artChanged)
        {
            AssetDatabase.SaveAssets();
        }

        return applied;
    }

    private static Dictionary<string, RectTransform> BuildSceneRectIndex()
    {
        Dictionary<string, RectTransform> index = new Dictionary<string, RectTransform>();
        RectTransform[] all = Resources.FindObjectsOfTypeAll<RectTransform>();
        foreach (RectTransform rect in all)
        {
            // 跳过 prefab 资产等不属于场景的对象（inactive 界面也能找到）。
            if (!rect.gameObject.scene.IsValid())
            {
                continue;
            }

            index[GetPath(rect.transform)] = rect;
        }

        return index;
    }

    private static NodeSnapshot ToSceneSnapshot(string path, RectTransform rect)
    {
        return new NodeSnapshot
        {
            path = path,
            target = (int)SyncTarget.SceneRect,
            anchorMin = rect.anchorMin,
            anchorMax = rect.anchorMax,
            pivot = rect.pivot,
            anchoredPosition = rect.anchoredPosition,
            sizeDelta = rect.sizeDelta,
            localScale = rect.localScale,
            localEuler = rect.localEulerAngles,
        };
    }

    private static NodeSnapshot ToArtLibrarySnapshot(SyncEntry entry, RectTransform rect)
    {
        // 美术库只存宽高不存缩放：把 localScale 折算进实际尺寸，回写后同步复位场景缩放，避免双重放大。
        Vector2 effectiveSize = new Vector2(
            rect.sizeDelta.x * rect.localScale.x,
            rect.sizeDelta.y * rect.localScale.y);
        return new NodeSnapshot
        {
            path = entry.path,
            target = (int)SyncTarget.ArtLibrary,
            positionField = entry.positionField,
            sizeField = entry.sizeField,
            anchoredPosition = rect.anchoredPosition,
            sizeDelta = effectiveSize,
        };
    }

    private static bool ApplyToSceneRect(RectTransform rect, NodeSnapshot snapshot)
    {
        bool changed = false;

        if (Changed(rect.anchorMin, snapshot.anchorMin)) { rect.anchorMin = snapshot.anchorMin; changed = true; }
        if (Changed(rect.anchorMax, snapshot.anchorMax)) { rect.anchorMax = snapshot.anchorMax; changed = true; }
        if (Changed(rect.pivot, snapshot.pivot)) { rect.pivot = snapshot.pivot; changed = true; }
        if (Changed(rect.anchoredPosition, snapshot.anchoredPosition)) { rect.anchoredPosition = snapshot.anchoredPosition; changed = true; }
        if (Changed(rect.sizeDelta, snapshot.sizeDelta)) { rect.sizeDelta = snapshot.sizeDelta; changed = true; }
        if (Changed(rect.localScale, snapshot.localScale)) { rect.localScale = snapshot.localScale; changed = true; }
        if (Changed(rect.localEulerAngles, snapshot.localEuler)) { rect.localEulerAngles = snapshot.localEuler; changed = true; }

        if (changed)
        {
            EditorUtility.SetDirty(rect);
        }

        return changed;
    }

    private static bool ApplyToArtLibrary(NodeSnapshot snapshot)
    {
        MilkTeaArtLibrary art = FindArtLibrary();
        if (art == null)
        {
            Debug.LogWarning("[Play 调整同步] 找不到 MilkTeaArtLibrary 资产，跳过美术库回写。");
            return false;
        }

        SerializedObject so = new SerializedObject(art);
        SerializedProperty position = so.FindProperty(snapshot.positionField);
        SerializedProperty size = so.FindProperty(snapshot.sizeField);
        if (position == null || size == null)
        {
            Debug.LogWarning($"[Play 调整同步] 美术库缺少字段：{snapshot.positionField} / {snapshot.sizeField}");
            return false;
        }

        bool changed = false;
        if (Changed(position.vector2Value, snapshot.anchoredPosition))
        {
            position.vector2Value = snapshot.anchoredPosition;
            changed = true;
        }

        if (Changed(size.vector2Value, snapshot.sizeDelta))
        {
            size.vector2Value = snapshot.sizeDelta;
            changed = true;
        }

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        return changed;
    }

    private static MilkTeaArtLibrary FindArtLibrary()
    {
        if (cachedArtLibrary == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:MilkTeaArtLibrary");
            if (guids.Length > 0)
            {
                cachedArtLibrary = AssetDatabase.LoadAssetAtPath<MilkTeaArtLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        return cachedArtLibrary;
    }

    private static bool Changed(Vector2 current, Vector2 snapshot)
    {
        return (current - snapshot).sqrMagnitude > Epsilon * Epsilon;
    }

    private static bool Changed(Vector3 current, Vector3 snapshot)
    {
        return (current - snapshot).sqrMagnitude > Epsilon * Epsilon;
    }

    private static RectTransform FindByPath(string path)
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in all)
        {
            // 跳过 prefab 资产等不属于场景的对象（inactive 界面也能找到）。
            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            if (GetPath(candidate) == path)
            {
                return candidate as RectTransform;
            }
        }

        return null;
    }

    private static string GetPath(Transform node)
    {
        string path = node.name;
        Transform current = node;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    private static string SnapshotFilePath()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(projectRoot, SnapshotFileName);
    }
}
