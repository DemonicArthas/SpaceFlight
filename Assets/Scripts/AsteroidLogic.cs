using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidLogic : MonoBehaviour
{
    [SerializeField]
    AudioClip destroyedSound;

    float randomRotation;

    private void Start()
    {
        randomRotation = Random.Range(0, 1f); ;
    }
    private void Update()
    {
        transform.Rotate(randomRotation, 0, 0);
    }

    public void ReceiveDamage()
    {
        AudioSource.PlayClipAtPoint(destroyedSound, transform.position);
        Destroy(gameObject);
    }
}
