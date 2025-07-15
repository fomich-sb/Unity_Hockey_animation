using UnityEngine;

public class ArenaOverflow : MonoBehaviour
{
    [SerializeField] private GameController gameController;

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Puck") || other.gameObject.CompareTag("Player"))
        {
            gameController.RestartGame(); 
        }
    }
}
