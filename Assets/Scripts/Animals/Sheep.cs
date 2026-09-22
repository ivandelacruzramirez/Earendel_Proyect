using UnityEngine;
using System.Collections.Generic;

public class Sheep : AnimalBase
{
    [Header("Sheep - Lana")]
    [SerializeField] private float woolAmount = 0f;
    [SerializeField] private float maxWool = 100f;
    [SerializeField] private float woolGrowthRate = 0.3f;
    [SerializeField] private AudioClip baaSound;

    [Header("Sheep - Rebaño")]
    [SerializeField] private float flockRadius = 8f;
    [SerializeField] private float flockWeight = 0.5f;

    [Header("Sheep - Curiosidad por comida")]
    [SerializeField] private float foodCuriosityRadius = 6f;

    private AudioSource audioSource;
    private float baaTimer;
    private static readonly List<Sheep> allSheep = new List<Sheep>();

    public bool CanBeSheared => woolAmount >= maxWool * 0.9f;
    public float WoolPercent => woolAmount / maxWool;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();

        isCurious         = true;
        curiosityRadius   = foodCuriosityRadius;
        curiosityDuration = 3f;
        curiosityCooldown = 25f;

        maxStomachCapacity = 80f;
        hungerThreshold    = 25f;
        fullThreshold      = 75f;
        eatSpeed           = 8f;

        wanderRadius   = 10f;
        normalSpeed    = 1.3f;
        hungrySpeed    = 2.2f;
        fleeSpeed      = 4.5f;

        foodDetectionRadius = 10f;

        baaTimer = Random.Range(0f, 15f);
    }

    protected override void Start()
    {
        base.Start();
        allSheep.Add(this);
        woolAmount = Random.Range(0f, maxWool * 0.4f);
    }

    private void OnDestroy()
    {
        allSheep.Remove(this);
    }

    protected override void Update()
    {
        base.Update();
        GrowWool();
        HandleBaaing();
    }

    private void GrowWool()
    {
        if (HungerPercent > 0.3f)
            woolAmount = Mathf.Min(woolAmount + woolGrowthRate * Time.deltaTime, maxWool);
    }

    private void HandleBaaing()
    {
        baaTimer -= Time.deltaTime;
        if (baaTimer <= 0f)
        {
            baaTimer = Random.Range(10f, 20f);
            PlayBaa();
        }
    }

    private void PlayBaa(float pitchOverride = -1f)
    {
        if (audioSource == null || baaSound == null) return;
        audioSource.pitch = pitchOverride > 0f ? pitchOverride : Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(baaSound, Random.Range(0.75f, 1f));
    }

    protected override void SetWanderDestination()
    {
        Vector3 flockCenter = GetFlockCenter();
        Vector3 randomOffset = Random.insideUnitSphere * wanderRadius;
        randomOffset.y = 0f;
        Vector3 target = Vector3.Lerp(transform.position + randomOffset, flockCenter, flockWeight);

        if (UnityEngine.AI.NavMesh.SamplePosition(target, out UnityEngine.AI.NavMeshHit hit, wanderRadius, UnityEngine.AI.NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    private Vector3 GetFlockCenter()
    {
        Vector3 center = transform.position;
        int count = 1;

        foreach (Sheep s in allSheep)
        {
            if (s == this) continue;
            if (Vector3.Distance(transform.position, s.transform.position) <= flockRadius)
            {
                center += s.transform.position;
                count++;
            }
        }
        return center / count;
    }

    public float Shear()
    {
        if (!CanBeSheared) return 0f;
        float sheared = woolAmount;
        woolAmount = 0f;
        PlayBaa(1.4f);
        return sheared;
    }

    public override bool CanEat(FoodItem food)
    {
        return food.Type == FoodType.Vegetables;
    }

    protected override void OnStateEnter(AnimalState state)
    {
        switch (state)
        {
            case AnimalState.Fleeing:
                PlayBaa(1.3f);
                AlertFlock();
                break;

            case AnimalState.Eating:
                PlayBaa(0.85f);
                break;

            case AnimalState.CuriousAboutPlayer:
                PlayBaa();
                break;
        }
    }

    private void AlertFlock()
    {
        foreach (Sheep s in allSheep)
        {
            if (s == this) continue;
            if (Vector3.Distance(transform.position, s.transform.position) <= flockRadius)
                s.Flee(transform.position);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, flockRadius);
    }
}
