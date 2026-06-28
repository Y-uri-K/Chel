using UnityEngine;

/// <summary>
/// Покадровая анимация для лазерных эффектов. Может петлиться или проиграться один раз.
/// </summary>
public class LaserAnimation : MonoBehaviour
{
    public Sprite[] frames;
    public float frameTime = 0.05f;
    public bool loop = true;

    SpriteRenderer sr;
    float timer;
    int index;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (frames != null && frames.Length > 0 && sr != null)
            sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        timer += Time.deltaTime;
        if (timer >= frameTime)
        {
            timer = 0f;
            index++;

            if (index >= frames.Length)
            {
                if (!loop)
                {
                    Destroy(gameObject);
                    return;
                }
                index = 0;
            }

            if (sr != null)
                sr.sprite = frames[index];
        }
    }
}
