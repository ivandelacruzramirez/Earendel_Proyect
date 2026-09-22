using UnityEngine;

public enum FoodType
{
    Vegetables,
    Fruit,
    Meat
}

[CreateAssetMenu(fileName = "NewFoodData", menuName = "FarmVR/Food Data")]
public class FoodData : ScriptableObject
{
    public string foodName = "Comida";
    public FoodType foodType;
    [Tooltip("Multiplicador de nutrición. Remolacha = 1.5")]
    public float nutritionValue = 1f;
    public Sprite icon;
}

public class FoodItem : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private FoodData foodData;
    [SerializeField] private float maxAmount = 30f;
    [SerializeField] private float currentAmount;

    [Header("Visual (opcional)")]
    [Tooltip("Transform que se escalará según la cantidad restante")]
    [SerializeField] private Transform visualRoot;

    public FoodType Type          => foodData != null ? foodData.foodType       : FoodType.Vegetables;
    public string   FoodName      => foodData != null ? foodData.foodName       : "Desconocido";
    public float    NutritionMult => foodData != null ? foodData.nutritionValue : 1f;

    public bool IsAvailable  => currentAmount > 0f;
    public bool IsEmpty      => currentAmount <= 0f;
    public float FillPercent => currentAmount / maxAmount;

    protected virtual void Awake()
    {
        currentAmount = maxAmount;
    }

    public float Consume(float amount)
    {
        float actual = Mathf.Min(amount * NutritionMult, currentAmount);
        currentAmount = Mathf.Max(0f, currentAmount - amount);

        UpdateVisual();

        if (IsEmpty)
            Invoke(nameof(DestroyFood), 1.5f);

        return actual;
    }

    public void Refill(float amount = -1f)
    {
        currentAmount = amount < 0f ? maxAmount : Mathf.Min(currentAmount + amount, maxAmount);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (visualRoot == null) return;
        float s = Mathf.Lerp(0.05f, 1f, FillPercent);
        visualRoot.localScale = new Vector3(s, s, s);
    }

    private void DestroyFood() => Destroy(gameObject);
}
