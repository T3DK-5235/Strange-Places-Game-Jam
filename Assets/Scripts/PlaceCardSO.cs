using System.Collections;
using System.Collections.Generic;
using System.IO.Enumeration;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "PlaceCard", menuName = "Card Objects/PlaceCard")]
public class PlaceCardSO : ScriptableObject
{
    public int initialAssessedValue; // The amount of points the card is worth for scoring in the second part
    public Sprite placeCardSprite; // The visual of the place
    public string placeCardDesc; // The description of the place
    public string placeCardTitle; // The title of the place
}
