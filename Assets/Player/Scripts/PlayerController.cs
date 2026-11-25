using UnityEngine;

namespace EvolveGames
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("PlayerController")]
        [SerializeField] public Transform Camera;
        [SerializeField, Range(1, 10)] float walkingSpeed = 3.0f;
        [Range(0.1f, 5)] public float CroughSpeed = 1.0f;
        [SerializeField, Range(2, 20)] float RuningSpeed = 4.0f;
        [SerializeField, Range(0, 20)] float jumpSpeed = 6.0f;
        [SerializeField, Range(0.5f, 10)] float lookSpeed = 2.0f;
        [SerializeField, Range(10, 120)] float lookXLimit = 80.0f;
        [Space(20)]
        [Header("Advance")]
        [SerializeField] float RunningFOV = 65.0f;
        [SerializeField] float SpeedToFOV = 4.0f;
        [SerializeField] float CroughHeight = 1.0f;
        [SerializeField] float gravity = 20.0f;
        [SerializeField] float timeToRunning = 2.0f;
        [HideInInspector] public bool canMove = true;
        [HideInInspector] public bool CanRunning = true;

        [Space(20)]
        [Header("Stamina")]
        [SerializeField] float maxStamina = 100f;
        [SerializeField] float staminaDrainPerSecond = 20f;     // расход при беге
        [SerializeField] float staminaRegenPerSecond = 12f;     // восстановление
        [SerializeField] float regenDelay = 1.0f;               // пауза после бега до восстановления
        [SerializeField, Range(0f, 1f)] float runResumePercent = 0.25f; // после полного истощения снова можно бежать с этого уровня

        [Space(10)]
        [Header("Breathing")]
        [SerializeField] AudioSource breathingSource;           // клип отдышки (Loop), Output -> SFX
        [SerializeField, Range(0f, 1f)] float breathingStartPercent = 0.2f; // отдышка начинается при 20% стамины
        [SerializeField, Range(0f, 1f)] float breathingStopPercent = 0.5f;  // и затихает, когда восстановилось до 50%
        [SerializeField, Range(0f, 1f)] float breathingMaxVolume = 1f;
        [SerializeField] float breathingFadeSpeed = 1.5f;

        [Space(20)]
        [Header("Input")]
        [SerializeField] KeyCode CroughKey = KeyCode.LeftControl;

        [HideInInspector] public CharacterController characterController;
        [HideInInspector] public Vector3 moveDirection = Vector3.zero;
        bool isCrough = false;
        float InstallCroughHeight;
        float rotationX = 0;
        [HideInInspector] public bool isRunning = false;
        Vector3 InstallCameraMovement;
        float InstallFOV;
        Camera cam;
        [HideInInspector] public bool Moving;
        [HideInInspector] public float vertical;
        [HideInInspector] public float horizontal;
        [HideInInspector] public float Lookvertical;
        [HideInInspector] public float Lookhorizontal;
        float RunningValue;
        float installGravity;
        [HideInInspector] public float WalkingValue;

        // Стамина
        float stamina;
        float regenTimer;
        bool exhausted;   // стамина кончилась, бежать нельзя, пока не восстановится
        bool breathing;

        /// <summary>Стамина от 0 до 1 (для UI, если понадобится).</summary>
        public float Stamina01 => stamina / maxStamina;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            cam = GetComponentInChildren<Camera>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            InstallCroughHeight = characterController.height;
            InstallCameraMovement = Camera.localPosition;
            InstallFOV = cam.fieldOfView;
            RunningValue = RuningSpeed;
            installGravity = gravity;
            WalkingValue = walkingSpeed;

            stamina = maxStamina;
            if (breathingSource != null)
            {
                breathingSource.loop = true;
                breathingSource.playOnAwake = false;
                breathingSource.volume = 0f;
            }
        }

        void Update()
        {
            RaycastHit CroughCheck;

            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            isRunning = !isCrough && CanRunning && !exhausted && Input.GetKey(KeyCode.LeftShift);
            vertical = canMove ? (isRunning ? RunningValue : WalkingValue) * Input.GetAxis("Vertical") : 0;
            horizontal = canMove ? (isRunning ? RunningValue : WalkingValue) * Input.GetAxis("Horizontal") : 0;
            if (isRunning) RunningValue = Mathf.Lerp(RunningValue, RuningSpeed, timeToRunning * Time.deltaTime);
            else RunningValue = WalkingValue;
            float movementDirectionY = moveDirection.y;
            moveDirection = (forward * vertical) + (right * horizontal);

            if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
            {
                moveDirection.y = jumpSpeed;
            }
            else
            {
                moveDirection.y = movementDirectionY;
            }
            characterController.Move(moveDirection * Time.deltaTime);
            Moving = horizontal < 0 || vertical < 0 || horizontal > 0 || vertical > 0 ? true : false;

            UpdateStamina();

            if (Cursor.lockState == CursorLockMode.Locked && canMove)
            {
                Lookvertical = -Input.GetAxis("Mouse Y");
                Lookhorizontal = Input.GetAxis("Mouse X");

                rotationX += Lookvertical * lookSpeed;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                Camera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, Lookhorizontal * lookSpeed, 0);

                if (isRunning && Moving) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, RunningFOV, SpeedToFOV * Time.deltaTime);
                else cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, InstallFOV, SpeedToFOV * Time.deltaTime);
            }

            if (Input.GetKey(CroughKey))
            {
                isCrough = true;
                float Height = Mathf.Lerp(characterController.height, CroughHeight, 5 * Time.deltaTime);
                characterController.height = Height;
                WalkingValue = Mathf.Lerp(WalkingValue, CroughSpeed, 6 * Time.deltaTime);
            }
            else if (!Physics.Raycast(cam.transform.position, transform.TransformDirection(Vector3.up), out CroughCheck, 0.8f, 1))
            {
                if (characterController.height != InstallCroughHeight)
                {
                    isCrough = false;
                    float Height = Mathf.Lerp(characterController.height, InstallCroughHeight, 6 * Time.deltaTime);
                    characterController.height = Height;
                    WalkingValue = Mathf.Lerp(WalkingValue, walkingSpeed, 4 * Time.deltaTime);
                }
            }
        }

        void UpdateStamina()
        {
            bool draining = isRunning && Moving;

            if (draining)
            {
                stamina -= staminaDrainPerSecond * Time.deltaTime;
                regenTimer = regenDelay;
                if (stamina <= 0f)
                {
                    stamina = 0f;
                    exhausted = true;
                }
            }
            else if (regenTimer > 0f)
            {
                regenTimer -= Time.deltaTime;
            }
            else
            {
                stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * Time.deltaTime);
            }

            if (exhausted && Stamina01 >= runResumePercent) exhausted = false;

            UpdateBreathing();
        }

        void UpdateBreathing()
        {
            if (breathingSource == null) return;

            // гистерезис: включается при 20%, выключается при 50%, чтобы звук не мигал
            if (!breathing && Stamina01 <= breathingStartPercent) breathing = true;
            else if (breathing && Stamina01 >= breathingStopPercent) breathing = false;

            float target = breathing ? breathingMaxVolume : 0f;
            breathingSource.volume = Mathf.MoveTowards(breathingSource.volume, target, breathingFadeSpeed * Time.deltaTime);

            if (breathing && !breathingSource.isPlaying) breathingSource.Play();
            else if (!breathing && breathingSource.isPlaying && breathingSource.volume <= 0.001f) breathingSource.Stop();
        }
    }
}