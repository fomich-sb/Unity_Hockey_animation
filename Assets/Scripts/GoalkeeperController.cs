using System.Collections;
using UnityEngine;

public class GoalkeeperController : PlayerBaseController
{
    [SerializeField] private Vector3 startPosition;
    [HideInInspector] public GameController gameController;

    private PuckController puck;
    private Transform puckTransform;

    void Start()
    {
        StartCoroutine(needPass());
    }

    public override void Initialize()
    {
        puck = team.puck;
        puckTransform = team.puck.gameObject.GetComponent<Transform>();
        gameController = team.gameController;
    }

    void Update()
    {
        Move();
    }

    void Move()
    {
        Vector3 newPosition = startPosition;
        newPosition.z = gameController.WanderGoalkeeperRadius * Unity.Mathematics.math.max(-1, Unity.Mathematics.math.min(1, puckTransform.position.z / (gameController.FieldWidth / 4)));
        transform.position = newPosition;
        Vector3 newLookAtPosition = puckTransform.position;
        newLookAtPosition.y = transform.position.y;
        transform.LookAt(newLookAtPosition);
    }

    IEnumerator needPass()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            if (puck.currentPlayer == this)
                Pass();
        }
    }

    void Pass()
    {
        Vector3 newLookAtPosition = team.enemyGoal.position;
        newLookAtPosition.y = transform.position.y;
        transform.LookAt(newLookAtPosition);
        for (int i = 0; i < 15; i++)
        {
            PlayerBaseController target;
            if (gameController.prepareGoalTeam)
                target = gameController.prepareGoalTeam.players[Random.Range(0, gameController.prepareGoalTeam.players.Length)];
            else
                target = team.players[Random.Range(0, team.players.Length)];
            if (target is PlayerController)
            {
                PlayerController targetPlayer = (PlayerController)target;

                // Проверяем, проходит ли передача
                RaycastHit hit;
                bool isBlocked = Physics.Linecast(puckTransform.position, targetPlayer.puckHoldPosition.position, out hit, gameController.playerLayer);
                if (isBlocked && hit.collider.GetComponent<PlayerBaseController>().team != team && Random.value < 0.8)
                {
                    Debug.DrawLine(puckTransform.position, targetPlayer.puckHoldPosition.position, Color.yellow, 1.5f);
                    Debug.Log("Пас не проходит");
                    continue;
                }


                puck.Pass(targetPlayer.puckHoldPosition.position, gameController.PassPower);
                return;
            }
        }
    }
}