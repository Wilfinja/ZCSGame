using UnityEngine;

public class Microwave : MonoBehaviour
{
    public GameObject burritoPrefab;
    public Transform launchPoint;
    public float launchForce = 5f;

    public GameObject player;
    public float detectionRadius;

    private bool playerInRange;

    public void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    public void LaunchBurrito()
    {
        GameObject burrito = Instantiate(burritoPrefab, launchPoint.position, Quaternion.identity);
        Rigidbody2D rb = burrito.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(launchForce, 0f); // Launch to the right
        }
    }

    private void Update()
    {
        // Calculate distance between this sprite and the player
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= detectionRadius && !playerInRange)
        {
            PlayerEnter();
        }
        else if (distanceToPlayer > detectionRadius && playerInRange)
        {
            PlayerExit();
        }

    }

    private void PlayerExit()
    {
        playerInRange = false;  
    }

    private void PlayerEnter()
    {
        playerInRange = true;


        LaunchBurrito();
    }
    
}