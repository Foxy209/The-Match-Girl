using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSys : MonoBehaviour
{
    public enum MatchMode { Tag, Layer }

    [System.Serializable]
    public class Surface
    {
        public string name = "Surface";
        public string surfaceTag;       // для режима Tag
        public LayerMask layers;        // для режима Layer
        public AudioClip[] clips;
        [Range(0f, 2f)] public float volume = 1f;
    }

    [Header("Источник звука")]
    [SerializeField] private AudioSource audioSource;

    [Header("Поверхности")]
    [SerializeField] private MatchMode matchMode = MatchMode.Tag;
    [SerializeField] private Surface[] surfaces;
    [SerializeField] private Surface defaultSurface;   // если ничего не подошло
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayLength = 0.6f;   // запас под ногами

    [Header("Шаги")]
    [SerializeField] private float minSpeed = 0.2f;
    [SerializeField] private float walkStride = 1.6f;  // метров между шагами при ходьбе
    [SerializeField] private float runSpeedThreshold = 4f;
    [SerializeField] private float runStride = 2.2f;
    [SerializeField] private float runVolumeMultiplier = 1.3f;

    [Header("Вариации")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.93f, 1.07f);
    [SerializeField] private Vector2 volumeJitter = new Vector2(0.9f, 1f);

    private CharacterController controller;
    private float distanceAccumulated;
    private AudioClip lastClip;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (!controller.isGrounded) return;

        Vector3 v = controller.velocity;
        v.y = 0f;
        float speed = v.magnitude;

        if (speed < minSpeed)
        {
            distanceAccumulated = 0f;
            return;
        }

        bool running = speed >= runSpeedThreshold;
        float stride = running ? runStride : walkStride;

        distanceAccumulated += speed * Time.deltaTime;
        if (distanceAccumulated >= stride)
        {
            distanceAccumulated = 0f;
            PlayStep(running);
        }
    }

    // Можно вызывать и из Animation Event, если понадобится
    public void PlayStep(bool running = false)
    {
        Surface surface = FindSurface();
        if (surface == null || surface.clips == null || surface.clips.Length == 0) return;

        AudioClip clip = surface.clips[Random.Range(0, surface.clips.Length)];
        if (surface.clips.Length > 1 && clip == lastClip)
        {
            // не играем один и тот же звук два раза подряд
            clip = surface.clips[(System.Array.IndexOf(surface.clips, clip) + 1) % surface.clips.Length];
        }
        lastClip = clip;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        float vol = surface.volume * Random.Range(volumeJitter.x, volumeJitter.y);
        if (running) vol *= runVolumeMultiplier;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(vol));
    }

    private Surface FindSurface()
    {
        Vector3 origin = controller.bounds.center;
        float dist = controller.bounds.extents.y + rayLength;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, dist,
                             groundMask, QueryTriggerInteraction.Ignore))
            return defaultSurface;

        foreach (Surface s in surfaces)
        {
            if (matchMode == MatchMode.Tag)
            {
                if (!string.IsNullOrEmpty(s.surfaceTag) && hit.collider.tag == s.surfaceTag)
                    return s;
            }
            else
            {
                if ((s.layers.value & (1 << hit.collider.gameObject.layer)) != 0)
                    return s;
            }
        }
        return defaultSurface;
    }
}