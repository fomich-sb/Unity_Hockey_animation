using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : PlayerBaseController
{
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 attackPosition;
    [SerializeField] private PlayerController opponent;


    [HideInInspector] public GameController gameController;
    [HideInInspector] public Transform opponentDefendTransform;
    private Transform opponentTransform;
    private PuckController puck;
    private Transform puckTransform;
    private NavMeshAgent agent;
    private Transform enemyGoal;
    private float nextDecisionTime;
    private float nextDecisionMoveTime; 

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (opponent)
        {
            opponentTransform = opponent.transform;
            opponent.opponentDefendTransform = this.transform;
        }
    }

    public override void Initialize()
    {
        puck = team.puck;
        puckTransform = team.puck.gameObject.GetComponent<Transform>();
        puck.OnStatusChange += PuckStatusChange;

        gameController = team.gameController;
        gameController.OnStatusChange += OnGameStatusChange;

        enemyGoal = team.enemyGoal;

        StartCoroutine(CheckDestinationReached());
    }

    void Update()
    {
        if (Time.time >= nextDecisionMoveTime)
            nextDecisionMoveTime = Time.time + MoveDecision();

        if (gameController.status != 2) return;

        if (Time.time >= nextDecisionTime)
        {
            nextDecisionTime = Time.time + gameController.DecisionInterval;
            if (puck.currentPlayer == this)
            {
                ActionDecision(); // Принять решение: удержать, пасовать или бить
            }
        }

    }

    void PuckStatusChange()
    {
        MoveDecision();
    }

    private void OnGameStatusChange(int status, bool immediately)
    {
        if (immediately && status == 1)
            transform.position = startPosition;
    }

    float MoveDecision()
    {
        agent.speed = gameController.PlayerSpeed;
        if (gameController.status==0)
        {
            agent.SetDestination(team.teamBench.position);
            return gameController.DecisionMoveInterval;
        }
        if (gameController.status == 1)
        {
            agent.SetDestination(startPosition);
            return gameController.DecisionMoveInterval;
        }
        if (puck.currentPlayer == null && puck.nearestPlayer[team.num-1] == this && puck.goalShootTeam == null) // шайба свободна. Это ближний к шайбе игрок команды
        {
            agent.SetDestination(getPuckPosition());
            return gameController.DecisionMoveInterval / 5;
        }
        if (puck.currentTeam != team) // шайба у другой команды
        {
            if (puck.currentPlayer == opponent || puck.currentPlayer is PlayerController && puck.nearestPlayer[team.num - 1] == this) // шайба у оппонента или ты ближе к противнику
            {
                agent.SetDestination(getActiveDefendPosition());
                agent.speed = gameController.PlayerSpeed * 1.2f;
                return gameController.DecisionMoveInterval / 5;
            }

            agent.SetDestination(getPassiveDefendPosition()); 
            return gameController.DecisionMoveInterval;
        }
        else // шайба у нашей команды
        {
            if (puck.currentPlayer == this) // игрок с шайбой
            {
                agent.SetDestination(getActiveAttackPosition());
                agent.speed = gameController.PlayerSpeed * 0.8f;
                return gameController.DecisionMoveInterval;
            }

            // Свободное движение в атаке
            agent.SetDestination(getPassiveAttackPosition());
            return gameController.DecisionMoveInterval;
        }
    }

    void ActionDecision()
    {
        float distanceToGoal = Vector3.Distance(transform.position, enemyGoal.position);
        float shootChance = 0;
        // Вероятность броска растет ближе к воротам
        if (gameController.prepareGoalTeam == null && distanceToGoal < gameController.MaxDistanceToShoot) {
            shootChance = (1f - distanceToGoal / gameController.MaxDistanceToShoot)*2f;
        }

        float passChance = 1f;
        if (gameController.prepareGoalTeam != null && gameController.prepareGoalTeam != team)
        {
            passChance = 10;
        }
        else if (opponentDefendTransform)
        {
            float opponentDistance = Vector3.Distance(transform.position, opponentDefendTransform.position);
            if (opponentDistance < gameController.MaxEnemyDistanceToPass)
            {
                passChance += (1f - opponentDistance / gameController.MaxEnemyDistanceToPass);
            }
        }

        float randomChoice = Random.value * (0.5f + shootChance + passChance);

        if (randomChoice < shootChance)
            Shoot();
        else if (randomChoice < shootChance + passChance)
            Pass();
        // Иначе продолжает удерживать шайбу
    }

    Vector3 getActiveAttackPosition()
    {
        Vector3 newPosition = Vector3.Lerp(attackPosition, enemyGoal.position, 0.5f);
        newPosition += Random.insideUnitSphere * gameController.WanderAttackRadius;
        newPosition.y = 0;
        return newPosition;
    }

    Vector3 getPassiveAttackPosition()
    {
        Vector3 newPosition = attackPosition;
        newPosition += Random.insideUnitSphere * gameController.WanderAttackRadius;
        newPosition.y = 0;
        return newPosition;
    }

    Vector3 getPuckPosition()
    {
        Vector3 newPosition = puckTransform.position;
        return newPosition;
    }
    Vector3 getActiveDefendPosition()
    {
        Vector3 newPosition = Vector3.Lerp(puck.currentPlayer.transform.position, team.teamGoal.position, gameController.ActiveDefendDistanceK);
        newPosition += Random.insideUnitSphere * (Vector3.Distance(puck.currentPlayer.transform.position, team.teamGoal.position)) * gameController.ActiveDefendDistanceK *0.7f;
        newPosition.y = 0;
        return newPosition;
    }

    Vector3 getPassiveDefendPosition()
    {
        Vector3 newPosition = Vector3.Lerp(opponentTransform.position, team.teamGoal.position, gameController.PassiveDefendDistanceK);
        newPosition += Random.insideUnitSphere * (Vector3.Distance(opponentTransform.position, team.teamGoal.position)) * gameController.PassiveDefendDistanceK * 0.7f;
        newPosition.y = 0;
        return newPosition;
    }


    IEnumerator CheckDestinationReached()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
   //         yield return new WaitUntil(() => agent.remainingDistance <= agent.stoppingDistance);
            yield return new WaitForEndOfFrame(); // Ждем завершения кадра
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    MoveDecision();
                }
            }
        }
    }

    void Pass()
    {
        for (int i = 0; i < 5; i++)
        {
            PlayerBaseController target;
            if(gameController.prepareGoalTeam)
                target = gameController.prepareGoalTeam.players[Random.Range(0, gameController.prepareGoalTeam.players.Length)];
            else
                target = team.players[Random.Range(0, team.players.Length)];

            if (target is PlayerController)
            {
                PlayerController targetPlayer = (PlayerController)target;
                if (targetPlayer == this) continue;

                //Нельзя дать пас назад
                Vector3 directionToTarget = targetPlayer.puckHoldPosition.position - transform.position;
                Vector3 currentDirection = transform.forward;
                float angle = Vector3.Angle(currentDirection, directionToTarget);
                if (Mathf.Abs(angle) > 70) continue;

                // Проверяем, проходит ли передача
                RaycastHit hit;
                bool isBlocked = Physics.Linecast(puckTransform.position, targetPlayer.puckHoldPosition.position, out hit, gameController.playerLayer);
                if (isBlocked && hit.collider.GetComponent<PlayerBaseController>().team != team)
                {
                    if (gameController.isDebug)
                    {
                        Debug.Log("Пас не проходит");
                        Debug.DrawLine(puckTransform.position, targetPlayer.puckHoldPosition.position, Color.yellow, 1.5f);
                    }
                    if (Random.value < 0.8)
                        continue;
                    Debug.Log("Пас не проходит, но решаемся");
                }

                puck.Pass(targetPlayer.puckHoldPosition.position, gameController.PassPower);

                if (gameController.isDebug)
                {
                    Debug.DrawLine(puckTransform.position, targetPlayer.puckHoldPosition.position, Color.red, 1.5f);
                    Debug.Log("Пас");
                }

                return;
            }
        }
    }

    public void Shoot()
    {
        Vector3 directionToTarget = enemyGoal.position - transform.position;
        Vector3 currentDirection = transform.forward;
        float angle = Vector3.Angle(currentDirection, directionToTarget);
        if (Mathf.Abs(angle) > 80) return;

        puck.Pass(enemyGoal.position, gameController.ShootPower);
        if (gameController.isDebug)
        {
            Debug.DrawLine(transform.position, enemyGoal.position, Color.black, 1.5f);
            Debug.Log("Удар");
        }
    }
}