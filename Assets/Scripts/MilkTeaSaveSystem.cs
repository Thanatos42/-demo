using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 存档底座：JSON 文件持久化，固定 3 个存档位（0 号为自动存档，每天进入休息界面时写入）。
/// 只保存"状态"（进度游标 + 已解锁内容 ID），不保存内容本身——剧情/角色/CG 都是资产，
/// 读档后按进度游标重新呈现。存档位于 Application.persistentDataPath/milktea_save_N.json。
/// </summary>
public static class MilkTeaSaveSystem
{
    /// <summary>一份存档的全部数据；新增持久化字段加在这里（JsonUtility 自动兼容缺省）。</summary>
    [Serializable]
    public sealed class SaveData
    {
        [Tooltip("当前章节索引（美术库 chapters 列表下标）")]
        public int chapterIndex;

        [Tooltip("当前天索引（章节 days 列表下标）")]
        public int dayIndex;

        [Tooltip("累计营业天数（标题栏“第 N 天”显示用）")]
        public int dayNumber = 1;

        [Tooltip("已解锁的内容 ID 集合（CG 等；格式如 cg:图名）")]
        public System.Collections.Generic.List<string> unlockedIds = new System.Collections.Generic.List<string>();

        [Tooltip("存档时间（显示用）")]
        public string savedAt = string.Empty;

        [Tooltip("存档摘要（存档位列表显示用，如“第一章 · 第 3 天”）")]
        public string label = string.Empty;
    }

    /// <summary>存档位数量；0 号固定为自动存档。</summary>
    public const int SlotCount = 3;
    public const int AutoSaveSlot = 0;

    private const string FileNameFormat = "milktea_save_{0}.json";

    public static string GetPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, string.Format(FileNameFormat, slot));
    }

    public static bool Exists(int slot)
    {
        return File.Exists(GetPath(slot));
    }

    public static void Save(int slot, SaveData data)
    {
        if (data == null)
        {
            return;
        }

        try
        {
            data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            File.WriteAllText(GetPath(slot), JsonUtility.ToJson(data, true), new UTF8Encoding(false));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("存档写入失败：" + exception.Message);
        }
    }

    public static SaveData Load(int slot)
    {
        try
        {
            string path = GetPath(slot);
            if (!File.Exists(path))
            {
                return null;
            }

            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("存档读取失败：" + exception.Message);
            return null;
        }
    }

    public static void Delete(int slot)
    {
        try
        {
            string path = GetPath(slot);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("存档删除失败：" + exception.Message);
        }
    }
}
