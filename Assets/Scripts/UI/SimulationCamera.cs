using UnityEngine;

namespace BattleRoyaleSim
{
    /// <summary>
    /// Cámara de control para el espectador.
    /// Permite navegación libre aérea estilo RTS (WASD + Scroll + Giro con clic derecho),
    /// selección táctica de jugadores con clic izquierdo, y seguimiento suave en modo espectador.
    /// </summary>
    public class SimulationCamera : MonoBehaviour
    {
        public static SimulationCamera Instance { get; private set; }

        [Header("Modo de Seguimiento")]
        public Transform followTarget = null;
        public float followSmoothness = 8f;

        [Header("Controles 2D Ortográficos")]
        public float panSpeed = 45f;
        public float zoomSpeed = 30f;
        public float minOrthoSize = 10f;
        public float maxOrthoSize = 85f;
        public float currentOrthoSize = 45f;

        private Camera cam;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            cam = GetComponent<Camera>();

            Configure2DCamera();
        }

        public void Configure2DCamera()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null)
            {
                // Configurar cámara puramente 2D (Ortográfica cenital)
                cam.orthographic = true;
                cam.orthographicSize = currentOrthoSize;
                transform.rotation = Quaternion.Euler(90f, 0, 0); // Vista cenital 2D estricta
            }
        }

        private void Update()
        {
            HandlePlayerSelectionClick();

            if (followTarget != null)
            {
                UpdateFollowMode();
            }
            else
            {
                UpdateFreeMode();
            }
        }

        private void UpdateFollowMode()
        {
            if (followTarget == null || !followTarget.gameObject.activeInHierarchy)
            {
                followTarget = null;
                return;
            }

            Vector3 targetPos = new Vector3(followTarget.position.x, 50f, followTarget.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmoothness);
            transform.rotation = Quaternion.Euler(90f, 0, 0);

            // Zoom 2D con la rueda del ratón
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentOrthoSize = Mathf.Clamp(currentOrthoSize - scroll * zoomSpeed, minOrthoSize, maxOrthoSize);
                cam.orthographicSize = currentOrthoSize;
            }

            // Si el usuario presiona WASD, cancela el seguimiento y pasa a cámara libre 2D
            if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f)
            {
                followTarget = null;
            }
        }

        private void UpdateFreeMode()
        {
            float dt = Time.deltaTime;

            // Desplazamiento 2D en los ejes X y Z con WASD
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            Vector3 moveDir = new Vector3(h, 0, v).normalized;
            transform.position += moveDir * (panSpeed * (currentOrthoSize / 45f) * dt);

            // Mantener altura fija y rotación hacia abajo
            transform.position = new Vector3(transform.position.x, 50f, transform.position.z);
            transform.rotation = Quaternion.Euler(90f, 0, 0);

            // Zoom 2D ortográfico
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentOrthoSize = Mathf.Clamp(currentOrthoSize - scroll * zoomSpeed, minOrthoSize, maxOrthoSize);
                cam.orthographicSize = currentOrthoSize;
            }
        }

        private void HandlePlayerSelectionClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (UnityEngine.EventSystems.EventSystem.current != null &&
                    UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 200f))
                {
                    PlayerAgent player = hit.collider.GetComponentInParent<PlayerAgent>();
                    if (player != null)
                    {
                        if (SimulationHUD.Instance != null)
                        {
                            SimulationHUD.Instance.SelectPlayer(player);
                        }
                    }
                }
            }
        }

        public void FollowTarget(Transform target)
        {
            followTarget = target;
            currentOrthoSize = 25f; // Zoom cercano automático en modo espectador
            if (cam != null) cam.orthographicSize = currentOrthoSize;
        }

        public void ClearTarget()
        {
            followTarget = null;
            currentOrthoSize = 48f;
            if (cam != null) cam.orthographicSize = currentOrthoSize;
        }

        public void SetInitialOverview(Vector3 center)
        {
            followTarget = null;
            transform.position = new Vector3(center.x, 50f, center.z);
            transform.rotation = Quaternion.Euler(90f, 0, 0);
            currentOrthoSize = 55f;
            Configure2DCamera();
        }
    }
}

// Con apoyo de Gemini
