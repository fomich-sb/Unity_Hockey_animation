using UnityEngine;

public abstract class PlayerBaseController : MonoBehaviour
{
    public Transform puckHoldPosition; // ����� ��������� �����
    [HideInInspector] public TeamController team;



    public abstract void Initialize();
}