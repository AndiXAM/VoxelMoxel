using UnityEngine;

public class Death : MonoBehaviour
{

    public Canvas canvas;
    public GameObject currentPlayer;
    public GameObject playerCharacter;

    public void PlayerDeath() 
    {
        CharacterController controller = currentPlayer.GetComponent<CharacterController>();
        controller.enabled = false;
        canvas.enabled = true;
    }
}