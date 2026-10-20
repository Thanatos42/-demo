using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一段对话数据。按顺序播放每一行，点按钮进入下一行，最后一行点按钮触发后续流程；
/// 行数不限，说话人可任意切换（主角/客人/其他店员）。
/// 台词与说话人支持占位符：{customer}=顾客名、{drink}=饮品名、{sugar}=糖度、{ice}=冰度。
/// 每行还可附带表情差分、语音与演出效果，播到该行时依次触发。
/// 可在 Unity 中右键 Create → 奶茶店 Demo → 对话 创建并在 Inspector 里编辑。
/// </summary>
[CreateAssetMenu(fileName = "Dialogue", menuName = "奶茶店 Demo/对话", order = 2)]
public sealed class MilkTeaDialogue : ScriptableObject
{
    /// <summary>行内附加演出的类型；新增演出只需扩展此枚举与运行时对应分支。</summary>
    public enum LineEventType
    {
        PlaySfx,
        ShowCg,
        PlayAnimation,
    }

    /// <summary>一行台词的附加演出效果：播到该行时触发，可叠加多个。</summary>
    [Serializable]
    public sealed class LineEvent
    {
        public LineEventType type;

        [Tooltip("ShowCg/PlayAnimation 的目标名（CG 图名 / 动画资产名），批次三接入；PlaySfx 直接用下方音频")]
        public string targetName;

        [Tooltip("PlaySfx 事件直接引用的效果音")]
        public AudioClip audioClip;
    }

    [Serializable]
    public sealed class Line
    {
        [Tooltip("说话人，可用 {customer} 表示当前顾客名；与角色资产名一致时启用其立绘与表情差分")]
        public string speaker = "顾客";

        [TextArea(2, 4)]
        [Tooltip("台词，可用 {drink}/{sugar}/{ice}/{customer} 占位符")]
        public string text = string.Empty;

        [Tooltip("推进到下一句的按钮文字，留空默认“继续”")]
        public string buttonLabel = "继续";

        [Tooltip("本行表情名：在说话人角色的差分列表中查找并切换立绘；留空保持当前表情")]
        public string expression = string.Empty;

        [Tooltip("本行语音，播到该行时播放（独占通道，新语音打断上一句）；留空不播")]
        public AudioClip voiceClip;

        [Tooltip("本行附加演出（CG/动画/音效），可叠加多个")]
        public List<LineEvent> events = new List<LineEvent>();
    }

    public List<Line> lines = new List<Line>();
}
