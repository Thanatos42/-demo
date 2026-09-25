using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用 UI 序列帧播放器：挂在带 <see cref="Image"/> 的物体上，按帧率循环切换 sprite。
/// 帧列表为空（或仅 1 帧）时不做动画，可与静态立绘并存，由外部改回静态图。
/// 帧数据放在 ScriptableObject（如 <see cref="MilkTeaCustomer"/>）里，运行时经
/// <see cref="Play"/> 注入，因此重建场景不会丢。
/// </summary>
[RequireComponent(typeof(Image))]
public sealed class SpriteSequenceAnimator : MonoBehaviour
{
    [Tooltip("序列帧列表，留空或仅 1 帧则不播放动画")]
    public List<Sprite> frames = new List<Sprite>();

    [Tooltip("播放帧率（帧/秒）")]
    public float framesPerSecond = 8f;

    [Tooltip("是否循环播放")]
    public bool loop = true;

    [Tooltip("启用时自动从头播放")]
    public bool playOnEnable = true;

    private Image image;
    private int frameIndex;
    private float timer;
    private bool playing;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Restart();
        }
    }

    private void OnDisable()
    {
        playing = false;
    }

    /// <summary>用一组新帧重新配置并从头播放；帧不足 2 帧则停止，交回静态图控制。</summary>
    public void Play(IList<Sprite> newFrames, float fps, bool shouldLoop)
    {
        frames.Clear();
        if (newFrames != null)
        {
            for (int i = 0; i < newFrames.Count; i++)
            {
                if (newFrames[i] != null)
                {
                    frames.Add(newFrames[i]);
                }
            }
        }

        framesPerSecond = fps > 0f ? fps : framesPerSecond;
        loop = shouldLoop;
        Restart();
    }

    /// <summary>停止播放（保留当前显示的帧），交由外部设置静态立绘。</summary>
    public void StopPlaying()
    {
        playing = false;
    }

    /// <summary>清空帧并停止，避免下次重新启用时重播旧动画，交回静态立绘控制。</summary>
    public void Clear()
    {
        frames.Clear();
        playing = false;
    }

    private void Restart()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        frameIndex = 0;
        timer = 0f;
        playing = frames != null && frames.Count >= 2;
        if (image != null && frames != null && frames.Count > 0)
        {
            image.sprite = frames[0];
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
    }

    private void Update()
    {
        if (!playing || image == null || frames == null || frames.Count < 2)
        {
            return;
        }

        float step = framesPerSecond > 0f ? 1f / framesPerSecond : 0.125f;
        timer += Time.deltaTime;
        while (timer >= step)
        {
            timer -= step;
            frameIndex++;
            if (frameIndex >= frames.Count)
            {
                if (loop)
                {
                    frameIndex = 0;
                }
                else
                {
                    frameIndex = frames.Count - 1;
                    playing = false;
                    break;
                }
            }
        }

        image.sprite = frames[frameIndex];
    }
}
