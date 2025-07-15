using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Camera3DFitter : MonoBehaviour
{
    [Header("Target Settings")]
    public GameObject targetObject;
    public Vector3 sizeOffset = Vector3.one;
    public float minDistance = 1f;
    public float maxDistance = 100f;

    [Header("Camera Settings")]
    [Range(1f, 179f)] public float fieldOfView = 60f;
    public float smoothingSpeed = 5f;

    private Camera cam;
    private float targetDistance;
    private Vector3 targetPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fieldOfView;
        UpdateCameraPosition(true);
    }

    void LateUpdate()
    {
        UpdateCameraPosition();
    }

    void UpdateCameraPosition(bool immediate = false)
    {
        if (targetObject == null) return;

        Bounds bounds = CalculateBounds(targetObject);
        bounds.size += sizeOffset; // Добавляем отступы

        // Рассчитываем необходимую дистанцию с учетом соотношения сторон
        float screenRatio = (float)Screen.width / Screen.height;
        float distanceX = CalculateDistanceForDimension(bounds.size.x, screenRatio);
        float distanceY = CalculateDistanceForDimension(bounds.size.y, 1f);
        float distanceZ = CalculateDistanceForDimension(bounds.size.z, 1f);

        // Выбираем наибольшую дистанцию
        targetDistance = Mathf.Max(distanceX, distanceY, distanceZ);
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        // Плавное перемещение камеры
        targetPosition = bounds.center - transform.forward * targetDistance;

        if (immediate)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothingSpeed * Time.deltaTime);
        }
    }

    float CalculateDistanceForDimension(float size, float ratioFactor)
    {
        // Рассчитываем расстояние с учетом FOV и соотношения сторон
        float fovRadians = fieldOfView * Mathf.Deg2Rad;
        float distance = (size * 0.5f) / (Mathf.Tan(fovRadians * 0.5f) * ratioFactor);
        return distance;
    }

    Bounds CalculateBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        // Если нет рендереров, используем коллайдеры
        if (renderers.Length == 0)
        {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return bounds;
    }

    // Реакция на изменение размеров окна
    private Vector2 lastScreenSize;
    void Update()
    {
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        if (currentScreenSize != lastScreenSize)
        {
            UpdateCameraPosition();
            lastScreenSize = currentScreenSize;
        }
    }
}