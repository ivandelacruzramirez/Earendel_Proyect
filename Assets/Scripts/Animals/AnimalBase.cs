using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum AnimalState
{
    Idle,
    Wandering,
    SeekingFood,
    Eating,
    CuriousAboutPlayer,
    Fleeing,
    Sleeping
}

[RequireComponent(typeof(NavMeshAgent))]
public abstract class AnimalBase : MonoBehaviour
{
    [Header("Identity")]
    public string animalName = "Animal";

    [Header("Stomach & Hunger")]
    [SerializeField] protected float maxStomachCapacity = 100f;
    [SerializeField] protected float currentStomach = 50f;
    [SerializeField] protected float hungerRate = 2f;
    [SerializeField] protected float hungerThreshold = 30f;
    [SerializeField] protected float fullThreshold = 90f;
    [SerializeField] protected float eatSpeed = 10f;

    [Header("Movement")]
    [SerializeField] protected float wanderRadius = 10f;
    [SerializeField] protected float wanderInterval = 5f;
    [SerializeField] protected float normalSpeed = 1.5f;
    [SerializeField] protected float hungrySpeed = 2.5f;
    [SerializeField] protected float fleeSpeed = 4f;
    [SerializeField] protected float stoppingDistance = 0.5f;

    [Header("Curiosity")]
    [SerializeField] protected bool isCurious = false;
    [SerializeField] protected float curiosityRadius = 5f;
    [SerializeField] protected float curiosityDuration = 4f;
    [SerializeField] protected float curiosityCooldown = 15f;
    [SerializeField] protected float approachDistanceToPlayer = 2f;

    [Header("Detection")]
    [SerializeField] protected float foodDetectionRadius = 8f;
    [SerializeField] protected LayerMask foodLayer;
    [SerializeField] protected LayerMask playerLayer;

    protected NavMeshAgent agent;
    protected AnimalState currentState = AnimalState.Idle;
    protected FoodItem targetFood;
    protected Transform player;

    private float wanderTimer;
    private float curiosityTimer;
    private float curiosityCooldownTimer;
    private bool isOnCooldown = false;

    public float HungerPercent => currentStomach / maxStomachCapacity;
    public bool IsHungry => currentStomach <= hungerThreshold;
    public bool IsFull => currentStomach >= fullThreshold;
    public AnimalState CurrentState => currentState;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
    }

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        wanderTimer = Random.Range(0f, wanderInterval);
        StartCoroutine(HungerLoop());
    }

    protected virtual void Update()
    {
        UpdateCooldowns();
        UpdateStateMachine();
    }

    private void UpdateCooldowns()
    {
        if (isOnCooldown)
        {
            curiosityCooldownTimer -= Time.deltaTime;
            if (curiosityCooldownTimer <= 0f)
                isOnCooldown = false;
        }
    }

    private void UpdateStateMachine()
    {
        switch (currentState)
        {
            case AnimalState.Idle:
                HandleIdle();
                break;
            case AnimalState.Wandering:
                HandleWandering();
                break;
            case AnimalState.SeekingFood:
                HandleSeekingFood();
                break;
            case AnimalState.Eating:
                HandleEating();
                break;
            case AnimalState.CuriousAboutPlayer:
                HandleCuriosity();
                break;
            case AnimalState.Fleeing:
                HandleFleeing();
                break;
        }
    }

    protected virtual void HandleIdle()
    {
        agent.isStopped = true;

        if (IsHungry)
        {
            TransitionTo(AnimalState.SeekingFood);
            return;
        }

        if (TryCuriosity()) return;

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            wanderTimer = wanderInterval + Random.Range(-1f, 1f);
            TransitionTo(AnimalState.Wandering);
        }
    }

    protected virtual void HandleWandering()
    {
        agent.speed = normalSpeed;
        agent.isStopped = false;

        if (IsHungry)
        {
            TransitionTo(AnimalState.SeekingFood);
            return;
        }

        if (TryCuriosity()) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            TransitionTo(AnimalState.Idle);
            return;
        }

        if (!agent.hasPath)
            SetWanderDestination();
    }

    protected virtual void HandleSeekingFood()
    {
        agent.speed = hungrySpeed;
        agent.isStopped = false;

        if (IsFull)
        {
            targetFood = null;
            TransitionTo(AnimalState.Idle);
            return;
        }

        if (targetFood == null || !targetFood.IsAvailable)
        {
            targetFood = FindNearestFood();
            if (targetFood == null)
            {
                TransitionTo(AnimalState.Wandering);
                return;
            }
        }

        agent.SetDestination(targetFood.transform.position);

        float distToFood = Vector3.Distance(transform.position, targetFood.transform.position);
        if (distToFood <= stoppingDistance + 0.3f)
            TransitionTo(AnimalState.Eating);
    }

    protected virtual void HandleEating()
    {
        agent.isStopped = true;

        if (targetFood == null || !targetFood.IsAvailable)
        {
            TransitionTo(AnimalState.SeekingFood);
            return;
        }

        if (IsFull)
        {
            OnFinishedEating();
            TransitionTo(AnimalState.Idle);
            return;
        }

        float bite = eatSpeed * Time.deltaTime;
        float actualBite = targetFood.Consume(bite);
        currentStomach = Mathf.Min(currentStomach + actualBite, maxStomachCapacity);

        OnEatingTick(actualBite);

        if (targetFood.IsEmpty)
        {
            targetFood = null;
            if (!IsFull)
                TransitionTo(AnimalState.SeekingFood);
            else
                TransitionTo(AnimalState.Idle);
        }
    }

    protected virtual void HandleCuriosity()
    {
        if (player == null || IsHungry)
        {
            TransitionTo(AnimalState.Idle);
            return;
        }

        agent.isStopped = false;
        agent.speed = normalSpeed * 0.8f;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer > approachDistanceToPlayer)
            agent.SetDestination(player.position);
        else
            agent.isStopped = true;

        curiosityTimer -= Time.deltaTime;
        if (curiosityTimer <= 0f)
        {
            isOnCooldown = true;
            curiosityCooldownTimer = curiosityCooldown;
            TransitionTo(AnimalState.Idle);
        }
    }

    protected virtual void HandleFleeing()
    {
        agent.speed = fleeSpeed;
        agent.isStopped = false;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            TransitionTo(AnimalState.Idle);
    }

    private bool TryCuriosity()
    {
        if (!isCurious || isOnCooldown || player == null) return false;

        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer <= curiosityRadius)
        {
            curiosityTimer = curiosityDuration;
            TransitionTo(AnimalState.CuriousAboutPlayer);
            return true;
        }
        return false;
    }

    protected FoodItem FindNearestFood()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, foodDetectionRadius, foodLayer);
        FoodItem nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            FoodItem food = hit.GetComponent<FoodItem>();
            if (food != null && food.IsAvailable && CanEat(food))
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = food;
                }
            }
        }
        return nearest;
    }

    protected virtual void SetWanderDestination()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir += transform.position;

        if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    public void Flee(Vector3 threatPosition)
    {
        Vector3 fleeDir = (transform.position - threatPosition).normalized;
        Vector3 fleeTarget = transform.position + fleeDir * wanderRadius;

        if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);

        TransitionTo(AnimalState.Fleeing);
    }

    protected void TransitionTo(AnimalState newState)
    {
        OnStateExit(currentState);
        currentState = newState;
        OnStateEnter(newState);
    }

    private IEnumerator HungerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            currentStomach = Mathf.Max(0f, currentStomach - hungerRate);

            if (IsHungry && currentState == AnimalState.Wandering)
                TransitionTo(AnimalState.SeekingFood);
        }
    }

    protected virtual void OnStateEnter(AnimalState state) { }
    protected virtual void OnStateExit(AnimalState state) { }
    protected virtual void OnEatingTick(float amount) { }
    protected virtual void OnFinishedEating() { }

    public abstract bool CanEat(FoodItem food);

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, foodDetectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, curiosityRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}
