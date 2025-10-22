using TMPro;
using UnityEngine;

[System.Serializable]
public class RequestToNextLV
{
    public int ColorID;
    public int requiredAmount;
    public int currentAmount;
    public TextMeshProUGUI textUI;

    public bool IsCompleted => currentAmount >= requiredAmount;
}
