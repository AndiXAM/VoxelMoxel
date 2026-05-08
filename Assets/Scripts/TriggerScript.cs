using UnityEngine;

public class TriggerScript : MonoBehaviour
{

    [SerializeField] private TitleScript titleControl;

    [SerializeField] private AudioSource audioControl;
    [SerializeField] private AudioClip music;

    public string mainText;
    public string subText;
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player")){
            titleControl.StartAnimation(mainText, subText);
            audioControl.Stop();
            audioControl.PlayOneShot(music);
        }
    }
}
