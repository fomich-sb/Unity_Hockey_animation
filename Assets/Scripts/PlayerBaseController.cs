using UnityEngine;

public abstract class PlayerBaseController : MonoBehaviour
{
    public Transform puckHoldPosition; // Точка крепления шайбы
    [HideInInspector] public TeamController team;



    public abstract void Initialize();
}