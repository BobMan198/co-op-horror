using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class EventWallManager : MonoBehaviour
{
    private AudioSource eventSound = null;

    public float soundRange = 25f;

    public float pointsAvailable;
    public float pointsPerTick;
    public float maxPointsAvailable;
    private bool eventUsed;

    // Start is called before the first frame update
    void Start()
    {
        pointsAvailable = maxPointsAvailable;
    }

    // Update is called once per frame
    void Update()
    {
        if (pointsAvailable <= 0 && !eventUsed)
        {
            eventSound = FindAnyObjectByType<Tablet>().GetComponent<AudioSource>();
            if (eventSound.isPlaying) return;
            eventSound.volume = 0.4f;
            eventSound.Play();
            eventUsed = true;
        }
    }
}
