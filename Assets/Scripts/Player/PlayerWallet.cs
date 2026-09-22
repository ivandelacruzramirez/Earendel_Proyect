using UnityEngine;
using UnityEngine.UI; 

public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance;

    [Header("Monedas")]
    public int currentCoins = 100;
    public Text coinsText; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    public bool SpendCoins(int amount)
{
    return TrySpendCoins(amount);
}

    public bool TrySpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (coinsText != null)
        {
            coinsText.text = "Wallet: " + currentCoins.ToString();
        }
    }
}