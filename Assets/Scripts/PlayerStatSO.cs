using System.Collections;
using System.Collections.Generic;
using System.IO.Enumeration;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStat", menuName = "Stat Objects/PlayerStat")]
public class PlayerStatSO : ScriptableObject
{
    [SerializeField] public int money; //Not relevant for initial game build
    [SerializeField] public List<GameObject> ownedPlaceCards;
    [SerializeField] public List<GameObject> ownedMoneyCards;
    [SerializeField] public Sprite playerIcon;
    [SerializeField] public bool gotCardInRound = false;
}
