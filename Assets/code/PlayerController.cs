using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform model;        // сюда перетащить Model
    [SerializeField] private Transform cameraTransform; // сюда перетащить Main Camera
    [SerializeField] private Animator animator;      // Animator модели (если пусто — найдётся в Model)

    [Header("Движение")]
    [SerializeField] private float walkSpeed = 1.2f; // подберите под анимацию, см. clipNativeSpeed
    [SerializeField] private float runSpeed = 2.4f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    [Header("Анимация")]
    [Tooltip("Скорость походки в клипе, м/с. Blender-скрипт пишет её в консоль: '*** скорость походки в клипе ≈ ...'")]
    [SerializeField] private float clipNativeSpeed = 0.8f;
    [SerializeField] private float speedDamp = 0.08f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AnimSpeedHash = Animator.StringToHash("AnimSpeed");

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (animator == null && model != null)
            animator = model.GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false; // движение делает CharacterController
    }

    private void Update()
    {
        // Ввод (старый Input Manager)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v).normalized;

        // Направление относительно камеры
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * input.z + camRight * input.x;

        // Скорость: бег на Shift
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // Поворот модели в сторону движения
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            model.rotation = Quaternion.Slerp(model.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Гравитация
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        UpdateAnimator(new Vector3(velocity.x, 0f, velocity.z).magnitude);
    }

    private void UpdateAnimator(float planarSpeed)
    {
        if (animator == null) return;

        // Speed > 0.1 -> Walk, иначе Idle (см. Animator Controller)
        animator.SetFloat(SpeedHash, planarSpeed, speedDamp, Time.deltaTime);

        // Скорость проигрывания ходьбы подгоняем под реальную скорость, чтобы ноги не скользили
        float animSpeed = planarSpeed > 0.01f
            ? Mathf.Clamp(planarSpeed / clipNativeSpeed, 0.6f, 2f)
            : 1f;
        animator.SetFloat(AnimSpeedHash, animSpeed);
    }
}
