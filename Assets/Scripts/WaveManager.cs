using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        public GameObject enemyPrefab;
        public int spawnPointIndex;       // Which spawn point to use
        public int count;                 // How many of this enemy to spawn
        public float delayBetweenSpawns;  // Seconds between each individual spawn
    }

    [System.Serializable]
    public class WaveData
    {
        public string waveName;           // e.g. "Wave 1"
        public List<EnemySpawnEntry> enemies = new List<EnemySpawnEntry>();
        public float maxWaveTime = 60f;   // Auto-advance after this many seconds
    }

    [Header("Wave Configuration")]
    public WaveData[] waves;

    [Header("Scene References")]
    public Transform[] spawnPoints;
    public GameObject levelEndObject;     // Activate this after final wave

    [Header("UI")]
    [Tooltip("Prefab with its own Canvas + styled TMP text. Instantiated at " +
             "runtime so there's no per-level Inspector wiring, and no risk " +
             "of a reference pointing at an object that only exists in a " +
             "different scene.")]
    public GameObject waveAnnouncementPrefab;
    public float announcementDuration = 3f;

    [Header("Between Waves")]
    public float betweenWaveDelay = 5f;   // Countdown after announcement before spawning

    // Runtime state
    private int currentWaveIndex = -1;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool waveInProgress = false;
    private float waveTimer = 0f;

    // Runtime-created announcement UI
    private GameObject waveAnnouncementInstance;
    private TextMeshProUGUI waveAnnouncementText;

    private void Awake()
    {
        if (levelEndObject != null)
            levelEndObject.SetActive(false);

        SetupAnnouncementUI();

        StartCoroutine(WaitForTransitionThenStart());
    }

    private void SetupAnnouncementUI()
    {
        if (waveAnnouncementPrefab == null)
        {
            Debug.LogWarning("[WaveManager] waveAnnouncementPrefab is not assigned — wave announcements will not be shown.");
            return;
        }

        waveAnnouncementInstance = Instantiate(waveAnnouncementPrefab);
        waveAnnouncementText = waveAnnouncementInstance.GetComponentInChildren<TextMeshProUGUI>();

        if (waveAnnouncementText == null)
            Debug.LogWarning("[WaveManager] waveAnnouncementPrefab has no TextMeshProUGUI in its children.");

        waveAnnouncementInstance.SetActive(false);
    }

    private IEnumerator WaitForTransitionThenStart()
    {
        // Don't show the first wave's announcement while the iris transition
        // is still covering the screen - it can eat the whole announcement
        // window on a slow scene load.
        if (SceneTransitionManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                SceneTransitionManager.Instance == null || !SceneTransitionManager.Instance.IsTransitioning);
        }

        yield return StartCoroutine(StartNextWave());
    }

    private void Update()
    {
        if (!waveInProgress) return;
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Length) return;

        // Clean up destroyed enemies from the list
        activeEnemies.RemoveAll(e => e == null);

        // Track wave timer
        waveTimer += Time.deltaTime;

        // Wave ends when all enemies dead OR timer expires
        bool allDead = activeEnemies.Count == 0;
        bool timedOut = waveTimer >= waves[currentWaveIndex].maxWaveTime;

        if (allDead || timedOut)
        {
            waveInProgress = false;

            if (currentWaveIndex >= waves.Length - 1)
            {
                // All waves complete
                StartCoroutine(LevelComplete());
            }
            else
            {
                StartCoroutine(StartNextWave());
            }
        }
    }

    private IEnumerator StartNextWave()
    {
        currentWaveIndex++;

        // Kill any leftover enemies from a timed-out wave
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();

        // Show announcement
        string waveName = currentWaveIndex < waves.Length
            ? waves[currentWaveIndex].waveName
            : "Final Wave";

        ShowAnnouncement(waveName + " Incoming!");
        yield return new WaitForSeconds(announcementDuration);

        // Countdown
        for (int i = (int)betweenWaveDelay; i > 0; i--)
        {
            ShowAnnouncement("Prepare... " + i);
            yield return new WaitForSeconds(1f);
        }

        HideAnnouncement();

        // Spawn the wave
        yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));

        waveTimer = 0f;

        // Guard: if every entry in this wave got skipped (bad prefab / bad
        // spawn index) there's nothing to fight, so treat it as complete
        // right away instead of flipping waveInProgress on and letting
        // Update() race it into the next wave a frame later.
        if (activeEnemies.Count == 0)
        {
            Debug.LogWarning($"[WaveManager] {waveName} spawned zero enemies — check prefab/spawn point assignments.");

            if (currentWaveIndex >= waves.Length - 1)
                StartCoroutine(LevelComplete());
            else
                StartCoroutine(StartNextWave());

            yield break;
        }

        waveInProgress = true;
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        foreach (EnemySpawnEntry entry in wave.enemies)
        {
            if (entry.enemyPrefab == null)
            {
                Debug.LogWarning($"[WaveManager] Null prefab in {wave.waveName}, skipping.");
                continue;
            }

            if (entry.spawnPointIndex < 0 || entry.spawnPointIndex >= spawnPoints.Length)
            {
                Debug.LogWarning($"[WaveManager] Invalid spawn point index {entry.spawnPointIndex} in {wave.waveName}.");
                continue;
            }

            Transform spawnPoint = spawnPoints[entry.spawnPointIndex];

            for (int i = 0; i < entry.count; i++)
            {
                GameObject enemy = Instantiate(entry.enemyPrefab, spawnPoint.position, Quaternion.identity);
                activeEnemies.Add(enemy);

                yield return new WaitForSeconds(entry.delayBetweenSpawns);
            }
        }
    }

    private IEnumerator LevelComplete()
    {
        ShowAnnouncement("All Waves Cleared!");
        yield return new WaitForSeconds(announcementDuration);
        HideAnnouncement();

        if (levelEndObject != null)
            levelEndObject.SetActive(true);
    }

    private void ShowAnnouncement(string message)
    {
        if (waveAnnouncementText == null || waveAnnouncementInstance == null) return;

        waveAnnouncementText.text = message;
        waveAnnouncementInstance.SetActive(true);
    }

    private void HideAnnouncement()
    {
        if (waveAnnouncementInstance == null) return;
        waveAnnouncementInstance.SetActive(false);
    }
}
