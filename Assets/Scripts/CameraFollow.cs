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

    [Header("Vertical Follow")]
    [SerializeField] float elevationThreshold = 25f;
    [SerializeField] float jumpVerticalSmoothTime = 0.06f;
    [SerializeField] float groundVerticalSmoothTime = 0.2f;
    [SerializeField] float jumpLookAhead = 0.45f;
    [SerializeField] float riseVelocityReference = 200f;

    Camera cam;
    PlayerController player;
    Rigidbody2D playerRb;
    float dampVelocityX;
    float dampVelocityY;
    float anchorPlayerY;
    bool anchorInitialized;
    float mapMinX;
    float mapMaxX;
    float mapMinY;
    float mapMaxY;
    bool hasBounds;
    bool canClampVertical;

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
        {
            player = target.GetComponent<PlayerController>();
            playerRb = target.GetComponent<Rigidbody2D>();
        }

        if (mapRoot == null)
        {
            var mapObject = GameObject.Find("map");
            if (mapObject != null)
                mapRoot = mapObject.transform;
        }

        CacheMapBounds();
        InitializeAnchor();
    }

    void InitializeAnchor()
    {
        if (target == null)
            return;

        anchorPlayerY = GetPlayerY();
        anchorInitialized = true;
    }

    float GetPlayerY()
    {
        if (playerRb != null)
            return playerRb.position.y;

        return target != null ? target.position.y : 0f;
    }

    void CacheMapBounds()
    {
        canClampVertical = false;

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

        float mapHeight = mapMaxY - mapMinY;
        canClampVertical = mapHeight > cam.orthographicSize * 2f;
    }

    float ComputeFramingY(float playerY, float halfHeight)
    {
        return playerY + offset.y + halfHeight * lookUpOffset * 2f;
    }

    void UpdateAnchorWhenGrounded(float playerY)
    {
        if (player == null || !player.IsGrounded)
            return;

        if (Mathf.Abs(player.VerticalVelocity) > 25f)
            return;

        float elevation = playerY - anchorPlayerY;
        if (elevation <= elevationThreshold)
            anchorPlayerY = playerY;
    }

    float ComputeDesiredY(float playerY, float halfHeight, float verticalVelocity, bool airborne)
    {
        float groundCameraY = ComputeFramingY(anchorPlayerY, halfHeight);
        float elevation = playerY - anchorPlayerY;

        if (elevation <= elevationThreshold)
            return groundCameraY;

        float desiredY = groundCameraY + elevation;

        if (airborne || verticalVelocity > 0f)
        {
            float riseFactor = Mathf.Clamp01(Mathf.Max(verticalVelocity, 0f) / riseVelocityReference);
            float airFactor = airborne ? Mathf.Max(riseFactor, 0.25f) : riseFactor;
            desiredY += halfHeight * jumpLookAhead * airFactor;
        }

        return desiredY;
    }

    void LateUpdate()
    {
        if (target == null || cam == null)
            return;

        if (!anchorInitialized)
            InitializeAnchor();

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float playerY = GetPlayerY();

        UpdateAnchorWhenGrounded(playerY);

        float verticalVelocity = player != null ? player.VerticalVelocity : 0f;
        bool airborne = player != null && !player.IsGrounded;
        float elevation = playerY - anchorPlayerY;
        bool followingElevation = elevation > elevationThreshold;

        var desired = target.position + offset;
        desired.y = ComputeDesiredY(playerY, halfHeight, verticalVelocity, airborne);
        desired.z = transform.position.z;

        if (hasBounds)
        {
            float minCameraX = mapMinX + halfWidth;
            float maxCameraX = mapMaxX - halfWidth;
            if (minCameraX > maxCameraX)
                maxCameraX = minCameraX;

            desired.x = Mathf.Clamp(desired.x, minCameraX, maxCameraX);

            if (canClampVertical && !followingElevation && !airborne)
            {
                float minCameraY = mapMinY + halfHeight;
                float maxCameraY = mapMaxY - halfHeight;
                desired.y = Mathf.Clamp(desired.y, minCameraY, maxCameraY);
            }
        }

        float verticalSmooth = followingElevation || airborne || verticalVelocity > 5f
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
