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
    public TextMeshProUGUI waveAnnouncementText;
    public float announcementDuration = 3f;

    [Header("Between Waves")]
    public float betweenWaveDelay = 5f;   // Countdown after announcement before spawning

    // Runtime state
    private int currentWaveIndex = -1;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool waveInProgress = false;
    private float waveTimer = 0f;

    private void Awake()
    {
        if (levelEndObject != null)
            levelEndObject.SetActive(false);

        if (waveAnnouncementText != null)
            waveAnnouncementText.gameObject.SetActive(false);

        // Start the first wave
        StartCoroutine(StartNextWave());
    }

    private void Update()
    {
        if (!waveInProgress) return;

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
        waveInProgress = true;
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        foreach (EnemySpawnEntry entry in wave.enemies)
        {
            if (entry.enemyPrefab == null)
            {
                Debug.LogWarning($"WaveManager: null prefab in {wave.waveName}, skipping.");
                continue;
            }

            if (entry.spawnPointIndex < 0 || entry.spawnPointIndex >= spawnPoints.Length)
            {
                Debug.LogWarning($"WaveManager: Invalid spawn point index {entry.spawnPointIndex} in {wave.waveName}.");
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
        if (waveAnnouncementText == null) return;
        waveAnnouncementText.text = message;
        waveAnnouncementText.gameObject.SetActive(true);
    }

    private void HideAnnouncement()
    {
        if (waveAnnouncementText == null) return;
        waveAnnouncementText.gameObject.SetActive(false);
    }
}
