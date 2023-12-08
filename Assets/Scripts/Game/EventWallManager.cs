using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class EventWallManager : MonoBehaviour
{
    public AudioSource eventSound = null;

    public float soundRange = 25f;

    public float pointsAvailable = 120;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (pointsAvailable == 0)
        {

            if (eventSound.isPlaying) return;

            eventSound.Play();

            pointsAvailable = 1;

            //var sound = new (transform.position, soundRange);

            //gameObject.SetActive(false);
        }
    }
}
