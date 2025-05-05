using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;

    [SerializeField]
    private float flashDuration = 0.1f;
    [SerializeField]
    private float intervalBetweenFlashes = 0.1f;
    [SerializeField]
    private Color flashColor = Color.white;
    [SerializeField]
    private int numberOfFlashes = 3;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void Flash()
    {
        //Debug.Log("Flashed");
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < numberOfFlashes; i++)
        {
            // Switch to flash color
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            // Switch back to original color
            spriteRenderer.color = originalColor;

            // Wait for interval if this isn't the last flash
            if (i < numberOfFlashes - 1)
            {
                yield return new WaitForSeconds(intervalBetweenFlashes);
            }
        }

        flashRoutine = null;
    }
}
