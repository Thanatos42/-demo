using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个可参与对话的"角色"：主角、客人与其他店员共用。
/// 角色只负责身份与立绘（含表情差分）；客人的点单与专属对话见 <see cref="MilkTeaCustomer"/>。
/// 把角色拖进 <see cref="MilkTeaArtLibrary"/> 的 characters 列表后，
/// 对话行的"说话人"与此角色名一致时，立绘与表情差分按名生效。
/// 可在 Unity 中右键 Create → 奶茶店 Demo → 角色 创建。
/// </summary>
[CreateAssetMenu(fileName = "Character", menuName = "奶茶店 Demo/角色", order = 4)]
public sealed class MilkTeaCharacter : ScriptableObject
{
    /// <summary>一条表情差分：名字 + 对应立绘。</summary>
    [Serializable]
    public sealed class Expression
    {
        [Tooltip("表情名，对话行里引用；建议首条设为“普通”作为默认表情")]
        public string name = "普通";

        [Tooltip("该表情对应的立绘")]
        public Sprite portrait;
    }

    [Header("身份")]
    [Tooltip("角色显示名；对话行的“说话人”与此名一致时启用本角色的立绘与表情差分")]
    public string characterName = "新角色";

    [Tooltip("角色默认立绘（表情名未命中差分时使用）")]
    public Sprite portrait;

    [Header("表情差分（可选，推荐首条为“普通”）")]
    [Tooltip("差分列表：对话行按表情名切换对应立绘")]
    public List<Expression> expressions = new List<Expression>();

    [Header("柜台小人（可选）")]
    [Tooltip("该角色站在柜台前显示的 Q 版小人")]
    public Sprite chibi;

    /// <summary>按表情名取差分立绘；表情名空或未命中返回 null。</summary>
    public Sprite GetPortrait(string expressionName)
    {
        if (string.IsNullOrEmpty(expressionName) || expressions == null)
        {
            return null;
        }

        for (int i = 0; i < expressions.Count; i++)
        {
            Expression entry = expressions[i];
            if (entry != null && entry.name == expressionName)
            {
                return entry.portrait;
            }
        }

        return null;
    }
}
