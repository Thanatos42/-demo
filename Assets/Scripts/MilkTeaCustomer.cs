using UnityEngine;

/// <summary>
/// 一位客人及其点单。把若干 <see cref="MilkTeaCustomer"/> 拖进
/// <see cref="MilkTeaArtLibrary"/> 的 customers 列表后，Demo 会按顺序让他们出场
/// （列表为空则回退为从配方里随机点单）。
/// 可在 Unity 中右键 Create → 奶茶店 Demo → 客人 创建。
/// </summary>
[CreateAssetMenu(fileName = "Customer", menuName = "奶茶店 Demo/客人", order = 3)]
public sealed class MilkTeaCustomer : ScriptableObject
{
    [Header("身份")]
    public string customerName = "顾客";

    [Tooltip("该客人的立绘，留空则用点单饮品或美术库中的默认立绘")]
    public Sprite portrait;

    [Header("点单")]
    [Tooltip("这位客人想要的饮品（决定茶底/奶底/配料的正确答案）")]
    public MilkTeaRecipe order;

    [Tooltip("糖度：0 无糖 / 1 少糖 / 2 多糖")]
    [Range(0, 2)]
    public int sugarLevel = 1;

    [Tooltip("冰度：0 去冰 / 1 少冰 / 2 多冰")]
    [Range(0, 2)]
    public int iceLevel = 1;

    [Header("对话（可选，留空用美术库中的默认对话）")]
    [Tooltip("进店点单时播放的对话")]
    public MilkTeaDialogue openingDialogue;

    [Tooltip("做对之后交付时播放的对话")]
    public MilkTeaDialogue servingDialogue;
}
