using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowableItem : MonoBehaviour
{
    public float throwForce = 10f;
    //public float damage = 2f;
    public Collider2D bounce;

    private Rigidbody2D rb;
    private bool isHeld = false;
    public float torqueForce;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Pickup(Transform holder)
    {
        bounce.enabled = false;
        isHeld = true;
        rb.isKinematic = true;
        transform.SetParent(holder);
        transform.localPosition = Vector3.zero;
    }

    public void Throw(Vector2 throwDirection)
    {
        if (!isHeld) return;

        StartCoroutine(ReactivateRB());
        isHeld = false;
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.AddForce(throwDirection * throwForce, ForceMode2D.Impulse);
        rb.AddTorque(torqueForce, ForceMode2D.Impulse);
    }

    IEnumerator ReactivateRB()
    {
        yield return new WaitForSeconds(.1f);
        bounce.enabled = true;
    }

    //private void OnCollisionEnter2D(Collision2D collision)
    //{
        // Damage enemy on impact
        //Dog enemy = collision.gameObject.GetComponent<Dog>();
        //if (enemy != null)
        //{
        //    enemy.TakeDamage(damage);
        //}
    //}
}
