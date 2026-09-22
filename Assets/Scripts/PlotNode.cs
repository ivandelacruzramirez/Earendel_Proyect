using UnityEngine;

public class PlotNode : MonoBehaviour
{
    [Header("Precios de Construcción")]
    public int huertoCost = 50;
    public int corralCost = 100;

    [Header("Prefabs a Construir")]
    public GameObject huertoPrefab;
    public GameObject corralPrefab;
    public GameObject menuUI; 

    private bool isOccupied = false;

    public void BuildHuerto()
    {
        if (isOccupied) return;

        if (PlayerWallet.Instance.SpendCoins(huertoCost))
        {
            Instantiate(huertoPrefab, transform.position, transform.rotation, transform);
            CompleteBuild();
        }
        else
        {
            Debug.Log("Cantidad insuficiente de monedas para el Huerto!");
        }
    }

    public void BuildCorral()
    {
        if (isOccupied) return;

        if (PlayerWallet.Instance.SpendCoins(corralCost))
        {
            Instantiate(corralPrefab, transform.position, transform.rotation, transform);
            CompleteBuild();
        }
        else
        {
            Debug.Log("Cantidad insuficiente de monedas para el Corral!");
        }
    }

    private void CompleteBuild()
    {
        isOccupied = true;
        if (menuUI != null) menuUI.SetActive(false); 
    }
}