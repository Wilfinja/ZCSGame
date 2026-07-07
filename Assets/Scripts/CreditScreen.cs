using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a full-screen panel in your Canvas.
/// Call Show() from RobotCEOBoss.Die() when the boss is defeated.
/// </summary>
public class CreditsScreen : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup canvasGroup;
    public Button returnToMenuButton;
    public string mainMenuSceneName = "MainMenu";

    [Header("Fade Settings")]
    public float fadeInDuration = 2f;
    public float delayBeforeFade = 1.5f;  // Wait after boss dies before fading in

    private void Awake()
    {
        // Start fully hidden
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (returnToMenuButton != null)
            returnToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    public void Show()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        gameObject.SetActive(true);

        yield return new WaitForSeconds(delayBeforeFade);

        // Fade the panel in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
