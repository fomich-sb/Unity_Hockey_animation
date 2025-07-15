using System.Collections;
using System.Diagnostics;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;

public class GameController : MonoBehaviour
{
    [SerializeField] TeamController team1;
    [SerializeField] TeamController team2;
    [SerializeField] public PuckController puck;
    [SerializeField] public Transform field;

    [SerializeField] public Light GoalLight1;
    [SerializeField] public Light GoalLight2;

    [SerializeField] public bool isDebug = false;
    [SerializeField] private float gameTimeScale = 1;
    [SerializeField] private Material[] team1Materials;
    [SerializeField] private Material[] team2Materials;
    public LayerMask playerLayer;


    [SerializeField] public float WanderAttackRadius = 5f; // Радиус случайного движения вокруг attackPosition
    [SerializeField] public float WanderGoalkeeperRadius = 1f; // Диапазон движения вратаря
    [SerializeField] public float DecisionInterval = 0.5f; // Как часто принимается решение (раз в секунду)
    [SerializeField] public float DecisionMoveInterval = 1f; // Как часто принимается решение по движению
    [SerializeField] public float PassiveDefendDistanceK = 0.3f; // Пассивная защита дистанция от игрока
    [SerializeField] public float ActiveDefendDistanceK = 0.1f; // Активная защита дистанция от игрока
    [SerializeField] public float ShootPower = 10f;
    [SerializeField] public float PassPower = 5f;
    [SerializeField] public float PlayerSpeed = 7;
    [SerializeField] public float TimeToPrepareGoal = 5f;

    [HideInInspector] public float FieldWidth;
    [HideInInspector] public float FieldLength;
    [HideInInspector] public float MaxDistanceToShoot;
    [HideInInspector] public float MaxEnemyDistanceToPass;

    public int status=1; // 0 - перерыв, 1 - на старт, 2 - игра
    [HideInInspector] public delegate void OnStatusChangeDelegate(int status, bool immediately);
    [HideInInspector] public event OnStatusChangeDelegate OnStatusChange;

    [SerializeField] public TeamController prepareGoalTeam = null;

    private InitPairData pairData = null;


    void Start()
    {
        team1.gameController = this;
        team1.num = 1;
        team1.enemyGoal = team2.teamGoal;
        team1.Initialize();

        team2.gameController = this;
        team2.num = 2;
        team2.enemyGoal = team1.teamGoal;
        team2.Initialize();

        puck.gameController = this;
        puck.teams[0] = team1;
        puck.teams[1] = team2;
        puck.Initialize();

        Time.timeScale = gameTimeScale;

        FieldWidth = field.localScale.z;
        FieldLength = field.localScale.x;
        MaxDistanceToShoot = FieldLength / 3f;
        MaxEnemyDistanceToPass = FieldWidth / 5;

        if (isDebug)
        {
            SetStatus1();
            Invoke(nameof(SetStatus2), 3f);
            StartCoroutine(PrepareGoalDebug());
        }
    }

    [Preserve]
    public void InitPair(string dataString)
    {
        pairData = JsonUtility.FromJson<InitPairData>(dataString);
        isDebug = false;
        InitPair2();
    }
    private void InitPair2()
    {
        Time.timeScale = pairData.TimeScale;
        TimeToPrepareGoal = pairData.TimeToPrepareGoal;
        Color newColor;
        if (HexToColor(pairData.Team1Color, out newColor))
        {
            foreach(Material teamMaterial in team1Materials)
              teamMaterial.color = newColor;
        }

        if (HexToColor(pairData.Team2Color, out newColor))
        {
            foreach (Material teamMaterial in team2Materials)
                teamMaterial.color = newColor;

        }
    }

    [Preserve]
    public void SetStatus(int status)
    {
        ChangeStatus(status);
    }

    void ChangeStatus(int newStatus, bool immediately = false)
    {
        if(pairData!=null)
            InitPair2();
        status = newStatus;
        OnStatusChange(status, immediately);
        prepareGoalTeam = null;
    }


    public void SetStatus0(bool immediately = false)
    {
        ChangeStatus(0, immediately);
    }

    public void SetStatus1(bool immediately=false)
    {
        ChangeStatus(1, immediately);
    }

    public void SetStatus2()
    {

        if (status == 1)
            ChangeStatus(2);
    }

    public void RestartGame()
    {
        if (status == 2)
        {
            SetStatus1(true);
            Invoke(nameof(SetStatus2), 1f);
        }
    }


    IEnumerator PrepareGoalDebug()
    {
        while (true) {
            yield return new WaitForSeconds(20f);
            if (!isDebug) 
                break;
            PrepareGoal(UnityEngine.Random.Range(1, 3));
            Invoke("ShootGoal2", TimeToPrepareGoal);
        }
    }

    void PrepareGoal(int teamNum)
    {
        if (teamNum == 1)
            prepareGoalTeam = team1;
        else
            prepareGoalTeam = team2;
    }

    void ShootGoal2()
    {
        ShootGoal();
    }

    void ShootGoal(int teamNum = -1)
    {
        if(teamNum == -1)
            puck.GoalShoot(prepareGoalTeam);
        else if(teamNum == 1)
            puck.GoalShoot(team1);
        else
            puck.GoalShoot(team2);
        prepareGoalTeam = null;
    }

    public void Goal(TeamController team)
    {
        if(isDebug)
            Invoke(nameof(RestartGame), 1f);
        if (team.num == 1)
            GoalLight2.enabled = true;
        else
            GoalLight1.enabled = true;
        Invoke(nameof(GoalLightDisable), 3f);
    }
    public void GoalLightDisable()
    {
        GoalLight1.enabled = false;
        GoalLight2.enabled = false;
    }

    // Преобразование HEX-строки в Color
    public static bool HexToColor(string hex, out Color color)
    {
        // Удаляем # если присутствует
        hex = hex.Replace("#", "");

        // Проверяем допустимую длину
        if (hex.Length != 3 && hex.Length != 6 && hex.Length != 8)
        {
            color = Color.white;
            return false;
        }

        try
        {
            // Разбираем HEX-строку
            byte r, g, b, a = 255;

            if (hex.Length == 3)
            {
                // Формат #RGB → #RRGGBB
                r = byte.Parse(hex.Substring(0, 1) + hex.Substring(0, 1), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(1, 1) + hex.Substring(1, 1), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(2, 1) + hex.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
            }
            else if (hex.Length == 6)
            {
                // Формат #RRGGBB
                r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            }
            else // hex.Length == 8
            {
                // Формат #RRGGBBAA
                r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                a = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            }

            // Создаем цвет (значения от 0 до 1)
            color = new Color32(r, g, b, a);
            return true;
        }
        catch
        {
            color = Color.white;
            return false;
        }
    }
}

[System.Serializable]
public class InitPairData
{
    public int TimeScale;
    public string Team1Color;
    public string Team2Color;
    public float TimeToPrepareGoal;
}