using UnityEngine;

public class TriggerScript2 : MonoBehaviour
{


    [SerializeField] private AudioSource audioControl;

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player")){
            audioControl.Stop();
        }
    }
}
