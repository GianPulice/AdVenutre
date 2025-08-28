using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public double money = 0;
    public TMP_Text moneyText;

    [Header("Clicker Config")]
    public double clickValue = 1; 

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Update()
    {
        moneyText.text = "$" + money.ToString("F0");
    }

    public void AddMoneyClicker()
    {
        money += clickValue;
    }
}
