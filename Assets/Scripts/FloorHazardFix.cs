using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HazardType
{
    Ice,        // Low drag (slippery)
    Mud,        // High drag (sticky)
    Oil,        // Very low drag
    Sand,       // Medium-high drag
    Custom      // Use custom drag value
}

public class FloorHazardFix : MonoBehaviour
{
    [Header("Hazard Configuration")]
    [SerializeField] private HazardType hazardType = HazardType.Mud;
    [SerializeField] private float customDragValue = 5f;
    [SerializeField] private float transitionSpeed = 2f;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem hazardParticles;
    [SerializeField] private Color hazardColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip exitSound;

    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Set visual appearance based on hazard type
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hazardColor;
        }
    }

    private float GetDragForHazardType()
    {
        switch (hazardType)
        {
            case HazardType.Ice:
                return 3f;
            case HazardType.Mud:
                return 50f;
            case HazardType.Oil:
                return 0.5f;
            case HazardType.Sand:
                return 30f;
            case HazardType.Custom:
                return customDragValue;
            default:
                return 1f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerDrag playerDrag = other.GetComponent<PlayerDrag>();
            if (playerDrag != null)
            {
                float dragValue = GetDragForHazardType();
                playerDrag.SetHazardDrag(dragValue, transitionSpeed);

                // Play enter effects
                if (hazardParticles != null)
                    hazardParticles.Play();

                if (audioSource != null && enterSound != null)
                    audioSource.PlayOneShot(enterSound);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerDrag playerDrag = other.GetComponent<PlayerDrag>();
            if (playerDrag != null)
            {
                playerDrag.RestoreNormalDrag(transitionSpeed);

                // Play exit effects
                if (hazardParticles != null)
                    hazardParticles.Stop();

                if (audioSource != null && exitSound != null)
                    audioSource.PlayOneShot(exitSound);
            }
        }
    }
}
