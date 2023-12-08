using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LiveCamera : MonoBehaviour
{
    private GameObject GameManagerPrefab;
    private GameObject EventWallManagerPrefab;
    public GameObject lastHit;
    private LayerMask eventLayer;
    public Vector3 collision = Vector3.zero;
    
    // Start is called before the first frame update
    void Start()
    {
        eventLayer = LayerMask.NameToLayer("EventRayCollider");
    }

    // Update is called once per frame
    void Update()
    {
        var ray = new Ray(this.transform.position, this.transform.forward);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, 10))
        {
            if (hit.transform.gameObject.layer == eventLayer)
            {
                Invoke("AddPointsByRay", 3);
                Debug.Log(hit.transform.gameObject);
            }
        }
    }

    public void AddPointsByRay()
    {
        GameManagerPrefab = GameObject.Find("/GameManager");
        EventWallManagerPrefab = GameObject.Find("/testwall/EventWall");

        if (EventWallManagerPrefab.GetComponent<EventWallManager>().pointsAvailable >= 10f)
        {
            GameManagerPrefab.GetComponent<GameRunner>().points += 10f;
            EventWallManagerPrefab.GetComponent<EventWallManager>().pointsAvailable -= 10f;
            Debug.Log("adding 10 points!");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(collision, 0.2f);
    }
}
