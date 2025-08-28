using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Business : MonoBehaviour
{
    [Header("Config")]
    public string businessName;
    public double baseCost = 10;
    public double baseProfit = 1;   // 👈 ahora es el profit base
    public float productionTime = 2f; // en segundos

    [Header("Estado")]
    public int level = 0;
    private bool isRunning = false;

    [Header("UI References (TMP)")]
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text costText;
    public TMP_Text profitText;
    public Button buyButton;
    public Button produceButton;  // 👈 botón manual (nivel < 10)
    public Image progressBar;

    private void Start()
    {
        nameText.text = businessName;
        buyButton.onClick.AddListener(Buy);
        produceButton.onClick.AddListener(StartProduction); // 👈 acción manual
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();

        // 👇 Si tiene nivel >= 10 y no está produciendo, arranca solo
        if (level >= 10 && !isRunning)
        {
            StartCoroutine(ProduceOnce());
        }
    }

    public void Buy()
    {
        double cost = GetCost();
        if (GameManager.instance.money >= cost)
        {
            GameManager.instance.money -= cost;
            level++;
            UpdateUI();
        }
    }

    private void StartProduction()
    {
        // 👇 Solo manual si nivel < 10
        if (level > 0 && !isRunning && level < 10)
        {
            StartCoroutine(ProduceOnce());
        }
    }

    private IEnumerator ProduceOnce()
    {
        isRunning = true;
        produceButton.interactable = false; // 👈 deshabilitamos hasta que termine

        float timer = 0;
        while (timer < productionTime)
        {
            timer += Time.deltaTime;
            if (progressBar != null)
                progressBar.fillAmount = timer / productionTime;
            yield return null;
        }

        // 🚀 ganancia por producción
        GameManager.instance.money += GetProfit();

        if (progressBar != null)
            progressBar.fillAmount = 0;

        isRunning = false;

        // 👇 reactivar el botón solo si es manual
        if (level < 10)
            produceButton.interactable = true;
        else
            produceButton.interactable = false; // a nivel 10+ no hace falta
    }

    public double GetCost()
    {
        int nextLevel = level + 1;
        return (baseCost + baseProfit) * nextLevel;
    }

    public double GetProfit()
    {
        return baseProfit * level;
    }

    private void UpdateUI()
    {
        levelText.text = "Lvl: " + level;
        costText.text = "$" + GetCost().ToString("F0");
        profitText.text = "$" + GetProfit().ToString("F0") + " / " + productionTime + "s";
        buyButton.interactable = GameManager.instance.money >= GetCost();

        // 👇 Manual disponible solo antes de nivel 10
        produceButton.interactable = (level > 0 && !isRunning && level < 10);
    }
}
