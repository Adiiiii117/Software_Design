using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ClearFaridUI : MonoBehaviour
{
    public RectTransform characterPanel;   // 動かすパネル
    public Image characterImage;           // Farid の画像

    public Sprite[] characterSprites;      // 0〜4 くらいで表情を入れる
    public int currentSpriteIndex = 0;

    public float slideDuration = 0.5f;
    public Vector2 hiddenPos = new Vector2(0f, -400f);
    public Vector2 shownPos = new Vector2(0f, 200f);

    public bool usePopScale = true;
    public float popScale = 1.1f;
    public float popDuration = 0.15f;

    public GameObject stageClearPanel;
    public float panelDelay = 0.1f;

    bool isPlaying = false;

    void Awake()
    {
        if (characterPanel == null)
            characterPanel = GetComponent<RectTransform>();

        if (characterPanel != null)
        {
            characterPanel.anchoredPosition = hiddenPos;
            characterPanel.localScale = Vector3.one;
        }

        if (characterImage != null && characterSprites.Length > 0)
        {
            currentSpriteIndex = Mathf.Clamp(currentSpriteIndex, 0, characterSprites.Length - 1);
            characterImage.sprite = characterSprites[currentSpriteIndex];
        }

        gameObject.SetActive(false);

        if (stageClearPanel != null)
            stageClearPanel.SetActive(false);
    }

    // RENJudge から呼んで表情を選ぶ
    public void SetImageByIndex(int index)
    {
        if (characterSprites == null || characterSprites.Length == 0) return;

        index = Mathf.Clamp(index, 0, characterSprites.Length - 1);
        currentSpriteIndex = index;

        if (characterImage != null)
            characterImage.sprite = characterSprites[currentSpriteIndex];
    }

    public void Play()
    {
        if (isPlaying) return;
        isPlaying = true;

        gameObject.SetActive(true);

        if (characterPanel != null)
        {
            characterPanel.anchoredPosition = hiddenPos;
            characterPanel.localScale = Vector3.one;
        }

        StartCoroutine(PlaySequenceRoutine());
    }

    IEnumerator PlaySequenceRoutine()
    {
        float t = 0f;

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;

            float ratio = Mathf.Clamp01(t / slideDuration);
            float eased = 1f - Mathf.Pow(1f - ratio, 3f);

            characterPanel.anchoredPosition =
                Vector2.Lerp(hiddenPos, shownPos, eased);

            yield return null;
        }

        characterPanel.anchoredPosition = shownPos;

        if (usePopScale)
        {
            float tt = 0f;
            Vector3 start = Vector3.one * popScale;
            Vector3 end = Vector3.one;

            characterPanel.localScale = start;

            while (tt < popDuration)
            {
                tt += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(tt / popDuration);
                float eased = 1f - Mathf.Pow(1f - ratio, 3f);

                characterPanel.localScale = Vector3.Lerp(start, end, eased);
                yield return null;
            }

            characterPanel.localScale = Vector3.one;
        }

        if (panelDelay > 0f)
            yield return new WaitForSecondsRealtime(panelDelay);

        if (stageClearPanel != null)
            stageClearPanel.SetActive(true);

        isPlaying = false;
    }
}
