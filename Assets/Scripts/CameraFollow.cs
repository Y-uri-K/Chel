using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Transform mapRoot;
    [SerializeField] Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] float lookUpOffset = 0.35f;
    [SerializeField] float smoothTime = 0.12f;

    [Header("Jump Follow")]
    [SerializeField] float jumpVerticalSmoothTime = 0.06f;
    [SerializeField] float groundVerticalSmoothTime = 0.2f;
    [SerializeField] float jumpLookAhead = 0.45f;
    [SerializeField] float riseVelocityReference = 200f;

    Camera cam;
    PlayerController player;
    float dampVelocityX;
    float dampVelocityY;
    float mapMinX;
    float mapMaxX;
    float mapMinY;
    float mapMaxY;
    bool hasBounds;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                target = playerObject.transform;
        }

        if (target != null)
            player = target.GetComponent<PlayerController>();

        if (mapRoot == null)
        {
            var mapObject = GameObject.Find("map");
            if (mapObject != null)
                mapRoot = mapObject.transform;
        }

        CacheMapBounds();
    }

    void CacheMapBounds()
    {
        if (cam == null || !cam.orthographic || mapRoot == null)
            return;

        var tilemap = mapRoot.GetComponentInChildren<Tilemap>();
        if (tilemap == null)
            return;

        tilemap.CompressBounds();
        var local = tilemap.localBounds;

        var worldMin = tilemap.transform.TransformPoint(local.min);
        var worldMax = tilemap.transform.TransformPoint(local.max);

        mapMinX = worldMin.x;
        mapMaxX = worldMax.x;
        mapMinY = worldMin.y;
        mapMaxY = worldMax.y;
        hasBounds = true;
    }

    void LateUpdate()
    {
        if (target == null || cam == null)
            return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float verticalVelocity = player != null ? player.VerticalVelocity : 0f;
        bool airborne = player != null && !player.IsGrounded;

        float lookOffset = lookUpOffset;
        if (airborne || verticalVelocity > 0f)
        {
            float riseFactor = Mathf.Clamp01(Mathf.Max(verticalVelocity, 0f) / riseVelocityReference);
            float airFactor = airborne ? Mathf.Max(riseFactor, 0.25f) : riseFactor;
            lookOffset += jumpLookAhead * airFactor;
        }

        var desired = target.position + offset;
        desired.y += halfHeight * lookOffset * 2f;
        desired.z = transform.position.z;

        if (hasBounds)
        {
            float minCameraY = mapMinY + halfHeight;
            float maxCameraY = mapMaxY - halfHeight;
            if (minCameraY > maxCameraY)
                maxCameraY = minCameraY;

            float minCameraX = mapMinX + halfWidth;
            float maxCameraX = mapMaxX - halfWidth;
            if (minCameraX > maxCameraX)
                maxCameraX = minCameraX;

            desired.x = Mathf.Clamp(desired.x, minCameraX, maxCameraX);
            desired.y = Mathf.Clamp(desired.y, minCameraY, maxCameraY);
        }

        float verticalSmooth = airborne || verticalVelocity > 5f
            ? jumpVerticalSmoothTime
            : groundVerticalSmoothTime;

        float newX = Mathf.SmoothDamp(
            transform.position.x,
            desired.x,
            ref dampVelocityX,
            smoothTime);

        float newY = Mathf.SmoothDamp(
            transform.position.y,
            desired.y,
            ref dampVelocityY,
            verticalSmooth);

        transform.position = new Vector3(newX, newY, desired.z);
    }
}
