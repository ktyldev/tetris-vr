﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explodable : MonoBehaviour {
    [SerializeField]
    private float explosionRadius = 5.0f;

    [SerializeField]
    private float explosionStrength = 100.0f;

    public void Explode(float time = 0.0f)
    {
        SoundEngine.GetEngine().PlaySFX(GameConstants.SFXExplosion);
        if (time > 0.0f)
        {
            StartCoroutine(ExplodeIn(time));
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            ~0 // all objects
        );

        foreach (var collider in hitColliders)
        {
            if (collider.gameObject == gameObject)
                continue;

            var diff = collider.transform.position - transform.position;
            var amt = 1.0f / Mathf.Pow((diff.magnitude + 1f), 2);

            var rb = collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 explosionForce = explosionStrength * amt * (diff.normalized);

                rb.AddForce(explosionForce);
            }

            /*
            var explodable = collider.GetComponent<Explodable>();
            if (explodable != null)
            {
                explodable.Explode(diff.magnitude / GameConstants.ExplosionTransferSpeed);
            }
            */

            var destroyable = collider.GetComponent<Destroyable>();
            if (destroyable != null)
            {
                destroyable.Destroy(DestructionMethod.Explosion);
            }
        }

        // now, destroy ourselves!
        /*
        gameObject
            .GetComponent<Destroyable>()
            .Destroy(DestructionMethod.Explosion);
            */
        ShowSFX();

        Destroy(this.gameObject);
    }

    public IEnumerator ExplodeIn(float time)
    {
        yield return new WaitForSeconds(time);
        this.Explode();
    }

    void ShowSFX()
    {
        var explosionFX = Instantiate(
            Resources.Load<GameObject>(
                GameConstants.ExplosionDebrisPrefab
            )
        );

        explosionFX.transform.position = transform.position;

        Destroy(explosionFX, 1.0f);
    }
}
