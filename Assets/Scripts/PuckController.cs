using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class PuckController : MonoBehaviour
{
    [SerializeField] private float detectNearestPlayerInterval = 1f;
    [SerializeField] private float battleInterval = 1f;
    [SerializeField] private float chanceInterception = 0.4f;
    [SerializeField] private float chanceTakePass = 0.9f;
    [SerializeField] private float chanceBattleWin = 0.3f;

    [HideInInspector] public TeamController currentTeam;
    [HideInInspector] public PlayerBaseController currentPlayer;
    private PlayerBaseController oldPlayer;
    
    [HideInInspector] public PlayerController[] nearestPlayer;
    [HideInInspector] public TeamController[] teams;
    [HideInInspector] public GameController gameController;
    [HideInInspector] public List<PlayerBaseController> playerInCollision = new List<PlayerBaseController>();

    [HideInInspector] public delegate void OnStatusChangeDelegate();
    [HideInInspector] public event OnStatusChangeDelegate OnStatusChange;

    private float nextBattleTime = 0;
    private float nextDetectNearestPlayerTime = 0;
    private float clearOldPlayerTime = 0;
    [HideInInspector] public TeamController goalShootTeam = null;
    private CapsuleCollider capsuleCollider;


    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        teams = new TeamController[2];
        nearestPlayer = new PlayerController[2];
    }

    public void Initialize()
    {
        gameController.OnStatusChange += OnGameStatusChange;
    }

    void Update()
    {
        if(Vector3.Distance(transform.position, gameController.transform.position)>100)
            gameController.RestartGame();

        if (oldPlayer != null && Time.time >= clearOldPlayerTime)
            oldPlayer = null;
        if (gameController.status != 2) return;

        if (goalShootTeam)
        {
            Vector3 targetGoal = goalShootTeam.enemyGoal.position;
            targetGoal.y = transform.position.y;
            if (transform.position == targetGoal)
                gameController.Goal(goalShootTeam);
            else
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetGoal,
                    Time.deltaTime * gameController.ShootPower * 7
                );
            }
        }
        else
        {

            if (currentPlayer != null)
                transform.position = Vector3.Lerp(transform.position, currentPlayer.puckHoldPosition.position, Time.deltaTime * 10f);

            if (Time.time >= nextDetectNearestPlayerTime)
                DetectNearestPlayer();

            if (Time.time >= nextBattleTime)
                BattleAction();
        }

        
    }

    private void OnGameStatusChange(int status, bool immediately)
    {

        if (status < 2)
        {
            capsuleCollider.enabled = false;
            SetStartPosition();
        }
        else
        {
            capsuleCollider.enabled = true;
        }

        if (status > 0)
            DetectNearestPlayer();
    }

    public void DetectNearestPlayer()
    {
        for (int i = 0; i < nearestPlayer.Length; i++)
        {
            float minDistance = Mathf.Infinity;
            foreach (var player in teams[i].players)
            {
                if (player is PlayerController)
                {

                    float distance = Vector3.Distance(transform.position, ((PlayerController)player).transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestPlayer[i] = (PlayerController)player;
                    }

                    if (distance<5 && !playerInCollision.Contains(player))
                    {
                        playerInCollision.Add(player);
                    }
                }
            }
        }
        nextDetectNearestPlayerTime = Time.time + detectNearestPlayerInterval;
    }

    private void OnCollisionExit(Collision collision)
    {
        PlayerBaseController player = collision.gameObject.GetComponent<PlayerBaseController>();
        if (player != null)
        {
            playerInCollision.Remove(player);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (goalShootTeam != null)
            return;

        PlayerBaseController player = collision.gameObject.GetComponent<PlayerBaseController>();
        if (player != null)
        {
            if(!playerInCollision.Contains(player))
                playerInCollision.Add(player);
            if (currentPlayer == player || oldPlayer == player)
                return;

            if(player is GoalkeeperController)
            {
                SetCurrentPlayer(player);//Вратарь забрал шайбу
                if(gameController.isDebug) Debug.Log("Вратарь забрал шайбу");
            }

            if (!currentTeam && (gameController.prepareGoalTeam == null || gameController.prepareGoalTeam == player.team)) //ничейная
            {
                SetCurrentPlayer(player);
                if (gameController.isDebug) Debug.Log("Подобрал шайбу");
                return;
            }

            if (currentTeam == player.team) //шайба наша
            {
                if (currentPlayer != null)
                    return;

                if (UnityEngine.Random.value < chanceTakePass && gameController.prepareGoalTeam == null || gameController.prepareGoalTeam == player.team )
                {
                    SetCurrentPlayer(player);//приняли пас
                    if (gameController.isDebug) Debug.Log("приняли пас");
                }
                else
                {
                    SetCurrentPlayer(null); //пропустили пас
                    if (gameController.isDebug) Debug.Log("пропустили пас");
                }

            }
            else //шайба у противника
            {
                if (currentPlayer != null)
                    BattleAction();

                if (currentPlayer == null && UnityEngine.Random.value < chanceInterception && gameController.prepareGoalTeam == null || gameController.prepareGoalTeam == player.team)
                {
                    SetCurrentPlayer(player);//перехват паса
                    if (gameController.isDebug) Debug.Log("перехват паса");
                }
                else
                {
                    if (gameController.isDebug) Debug.Log("нет перехвата паса");
                }
            }
            return;
        }
    }

    void BattleAction()
    {
        foreach (PlayerBaseController player in playerInCollision)
        {
            if (Vector3.Distance(player.transform.position, transform.position)>9) {
                playerInCollision.Remove(player);
                continue;
            }
            if (currentTeam == player.team && currentPlayer != null) //шайба наша
                continue;

            if(gameController.prepareGoalTeam == player.team)
                SetCurrentPlayer(player);


            if (currentPlayer == null)
            {
                if (UnityEngine.Random.value < chanceInterception)
                {
                    SetCurrentPlayer(player);
                    if (gameController.isDebug) Debug.Log("подобрали шайбу 2");
                }
                continue;
            }

            if (UnityEngine.Random.value < chanceBattleWin && gameController.prepareGoalTeam == null)
            {
                SetCurrentPlayer(player); //выиграли единоборство
                if (gameController.isDebug) Debug.Log("выиграли единоборство");
                continue;
            }
            if (gameController.isDebug) Debug.Log("проиграли единоборство");
            
        }
        nextBattleTime = Time.time + battleInterval;
    }

    void SetCurrentPlayer(PlayerBaseController player, TeamController team = null)
    {
        if (currentPlayer)
        {
            oldPlayer = currentPlayer;
            clearOldPlayerTime = Time.time + 0.5f;
        }

        currentPlayer = player;
        if (player != null)
        {
            currentTeam = player.team;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            currentTeam = team;
        }

        OnStatusChange();
    }


    
    public void Pass(Vector3 targetPosition, float force)
    {
        SetCurrentPlayer(null, currentTeam);
        Vector3 direction = (targetPosition - transform.position).normalized;
        rb.AddForce(direction * force, ForceMode.Impulse);
        if (gameController.isDebug)
            Debug.DrawLine(transform.position, transform.position + direction * force, Color.green, 2f);

    }
    public void GoalShoot(TeamController team)
    {
        goalShootTeam = team;
        SetCurrentPlayer(null, goalShootTeam);
        capsuleCollider.enabled = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void SetStartPosition()
    {
        SetCurrentPlayer(null, null);
        goalShootTeam = null;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 pos = transform.position;
        pos.x = 0;
        pos.z = 0;
        transform.position = pos;
    }
    
}