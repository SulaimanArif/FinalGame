using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static int goldCollected = 0;
    public static int diamondsCollected = 0;
    public static int enemiesKilled = 0;
    public static int carPartsCollected = 0;

    public const int totalGold = 25;
    public const int totalDiamonds = 10;
    public const int totalCarParts = 10;
    public const int totalEnemies = 40;

    public TextMeshProUGUI goldAmount;
    public TextMeshProUGUI diamondsAmount;
    public TextMeshProUGUI enemiesKilledAmount;
    public TextMeshProUGUI carPartsAmount;
}