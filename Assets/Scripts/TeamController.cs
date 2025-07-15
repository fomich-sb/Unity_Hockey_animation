using UnityEngine;

public class TeamController : MonoBehaviour
{
    public PlayerBaseController[] players;
    public Transform teamGoal;
    public Transform teamBench;

    [HideInInspector] public GameController gameController;
    [HideInInspector] public PuckController puck;
    [HideInInspector] public Transform enemyGoal;
    [HideInInspector] public int num;


    public void Initialize()
    {
        puck = gameController.puck;
        foreach (var item in players)
        {
            item.team = this;
            item.Initialize();
        }
    }
}