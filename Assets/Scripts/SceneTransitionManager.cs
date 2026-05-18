using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [Tooltip("How long the circle takes to close (seconds)")]
    public float closeSpeed = 0.6f;

    [Tooltip("How long the circle takes to open (seconds)")]
    public float openSpeed = 0.5f;

    [Tooltip("Brief pause when fully closed, before loading")]
    public float holdDuration = 0.1f;

    // The full 'black screen' size - should be large enough to cover any resolution.
    // Set to 2x the diagonal of your max resolution to be safe.
    private const float MAX_SCALE = 20f;

    private Canvas canvas;
    private Image irisImage;
    private RectTransform irisRect;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCanvas();

            // Register with your existing persistent object system
            if (PersistantObjDestroyer.Instance != null)
                PersistantObjDestroyer.Instance.RegisterPersistentObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildCanvas()
    {
        // Create a canvas that renders on top of everything
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Always on top

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // Create the iris circle image
        GameObject imageObj = new GameObject("IrisCircle");
        imageObj.transform.SetParent(canvas.transform, false);

        irisImage = imageObj.AddComponent<Image>();
        irisRect = imageObj.GetComponent<RectTransform>();

        // Black circle - Unity's built-in filled circle sprite
        // We'll use a programmatically generated circle texture
        irisImage.sprite = CreateCircleSprite(256);
        irisImage.color = Color.black;

        // Anchor to center of screen
        irisRect.anchorMin = new Vector2(0.5f, 0.5f);
        irisRect.anchorMax = new Vector2(0.5f, 0.5f);
        irisRect.pivot = new Vector2(0.5f, 0.5f);
        irisRect.sizeDelta = new Vector2(100f, 100f); // Base size, we'll scale it

        // Start fully open (scale 0 = invisible)
        irisRect.localScale = Vector3.zero;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Generates a circle texture at runtime - no sprite asset needed
    private Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        float center = resolution / 2f;
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                // Smooth edge
                float alpha = 1f - Mathf.Clamp01((dist - (radius - 2f)) / 2f);
                tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }

        tex.Apply();

        return Sprite.Create(tex,
            new Rect(0, 0, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Open the iris when the new scene is ready
        StartCoroutine(OpenIris());
    }

    /// <summary>
    /// Call this instead of SceneManager.LoadScene() to get the iris transition.
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        if (!isTransitioning)
            StartCoroutine(DoTransition(sceneName));
    }

    public void TransitionToScene(int sceneIndex)
    {
        if (!isTransitioning)
            StartCoroutine(DoTransition(sceneIndex));
    }

    private IEnumerator DoTransition(object sceneTarget)
    {
        isTransitioning = true;

        yield return StartCoroutine(CloseIris());

        yield return new WaitForSecondsRealtime(holdDuration);

        if (sceneTarget is string sceneName)
            SceneManager.LoadScene(sceneName);
        else if (sceneTarget is int sceneIndex)
            SceneManager.LoadScene(sceneIndex);

        // OpenIris is triggered automatically via OnSceneLoaded
    }

    private IEnumerator CloseIris()
    {
        // Move circle to player's screen position
        PositionOnPlayer();

        float elapsed = 0f;
        while (elapsed < closeSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / closeSpeed;

            // Ease in: starts slow, ends fast — feels like the circle is snapping shut
            float eased = t * t;
            irisRect.localScale = Vector3.one * Mathf.Lerp(MAX_SCALE, 0f, eased);

            // Keep tracking the player as the circle closes
            PositionOnPlayer();

            yield return null;
        }

        irisRect.localScale = Vector3.zero;
    }

    private IEnumerator OpenIris()
    {
        // Position on player in the new scene (may take a frame to find)
        yield return null;
        PositionOnPlayer();

        float elapsed = 0f;
        while (elapsed < openSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / openSpeed;

            // Ease out: starts fast, ends slow — gentle reveal
            float eased = 1f - (1f - t) * (1f - t);
            irisRect.localScale = Vector3.one * Mathf.Lerp(0f, MAX_SCALE, eased);

            yield return null;
        }

        irisRect.localScale = Vector3.one * MAX_SCALE;

        // Immediately snap invisible once fully open
        irisRect.localScale = Vector3.zero;
        isTransitioning = false;
    }

    private void PositionOnPlayer()
    {
        // Try to find the player in the current scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null || Camera.main == null)
        {
            // Default to screen center if player not found
            irisRect.anchoredPosition = Vector2.zero;
            return;
        }

        // Convert player world position to screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(player.transform.position);

        // Convert screen position to canvas local position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            new Vector2(screenPos.x, screenPos.y),
            canvas.worldCamera,
            out Vector2 localPos
        );

        irisRect.anchoredPosition = localPos;
    }
}
