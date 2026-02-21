using System.Collections;
using System.Collections.Generic;
using System.IO.Enumeration;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "MoneyCard", menuName = "Card Objects/MoneyCard")]
public class MoneyCardSO : ScriptableObject
{
    public int value; // the value of the card for final scoring
    public Sprite moneyCardSprite; // The background for the money cards
}
