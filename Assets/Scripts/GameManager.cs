using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class GameManager : MonoBehaviour
{
    //TODO these lists should always be a multiple of 4 (the player + 3 ais)

    [Header("Card Stuff")]
    [SerializeField] List<PlaceCardSO> inPlayPlaceCards; // List of Place cards being used in current round, that aren't yet owned by any "player"
    [SerializeField] List<MoneyCardSO> inPlayMoneyCards; // List of Money cards being used in current round, that aren't yet owned by any "player"
    [SerializeField] GameObject placeCardPrefab;
    [SerializeField] GameObject moneyCardPrefab;
    [SerializeField] GameObject cardContainer;

    [Header("Player Stuff")]
    [SerializeField] List<PlayerStatSO> playerList; //A list of the player + 3 other ai character's info
    [SerializeField] int[] playerTotalBuyIns;
    [SerializeField] GameObject playerContainer;
    [SerializeField] GameObject playerInfoPrefab;
    [SerializeField] int defaultStartingMoney = 21000;
    [SerializeField] GameObject biddingUI;
    [SerializeField] GameObject aiCardContainer;
    List<GameObject> playerInfo;

    [SerializeField] GameObject currentRoundTotalBidText;
    private int currentRoundTotalBid;

    [Header("User Stuff")]
    int userMoney;
    [SerializeField] GameObject playersOwnedCardsContainer;

    [Header("Do Not Assign In Editor")] // These are only serialized so it is easy to see what is going on
    [SerializeField] List<PlaceCardSO> shuffledPlaceCards;
    [SerializeField] List<PlaceCardSO> shuffledCards;

    private const int TOTAL_ROUND_NUM = 4;
    private const int PLAYER_NUM = 4;
    private int currentRoundNum;

    private int currentBidValue;
    private bool bidSent;
    private bool userBoughtIn;

    private const int AI_BID_CHANCE = 60;

    void Start()
    {
        shuffledPlaceCards = inPlayPlaceCards.OrderBy(x => Random.value).ToList();
        shuffledCards = inPlayPlaceCards.OrderBy(x => Random.value).ToList();

        currentRoundNum = 0;
        currentBidValue = 1000;

        bidSent = false;
        userBoughtIn = false;

        playerInfo = new List<GameObject>();

        playerTotalBuyIns = new int[4];

        //! resets scriptable objects, only required in editor.
        for(int i = 0; i < playerList.Count(); i++)
        {
            playerList[i].ownedPlaceCards.Clear();
        }

        InitPlayers();

        StartCoroutine(RunGameLoop());
    }

    // // reset scriptable objects on keypress
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.R))
    //     {
    //         for(int i = 0; i < playerList.Count(); i++)
    //         {
    //             playerList[i].ownedPlaceCards.Clear();
    //         }
    //     }
    // }

    private void InitPlayers()
    {
        for(int i = 0; i < PLAYER_NUM; i++)
        {
            GameObject newPlayerInfo = Instantiate(playerInfoPrefab, playerContainer.transform) as GameObject;
            
            Image playerIcon = newPlayerInfo.transform.GetChild(0).GetComponent<Image>();
            TextMeshProUGUI currentMoney = newPlayerInfo.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

            playerIcon.sprite = playerList[i].playerIcon;
            currentMoney.text = defaultStartingMoney.ToString();

            playerList[i].money = defaultStartingMoney;

            playerInfo.Add(newPlayerInfo);
        }
        
    }

    private IEnumerator RunGameLoop()
    {
        for(int i = 0; i < PLAYER_NUM; i++)
        {
            yield return StartCoroutine(InitAuctionRound());
        }

        biddingUI.SetActive(false);
        currentRoundTotalBidText.SetActive(false);

        playersOwnedCardsContainer.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
        playersOwnedCardsContainer.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);

        Vector3 newPos = new Vector3(140, -266, 0);
        playersOwnedCardsContainer.transform.localPosition = newPos;

        for(int i = 0; i < PLAYER_NUM; i++)
        {
            yield return StartCoroutine(InitSellingRound());
        }
    }

    //The Round where players and AI bid on properties
    private IEnumerator InitAuctionRound()
    {
        List<GameObject> dealtPlaceCards = new List<GameObject>();
        for (int i = 0; i < PLAYER_NUM; i++) { playerTotalBuyIns[i] = 0; } 

        currentRoundTotalBid = 1000;
        TextMeshProUGUI totalBidText = currentRoundTotalBidText.GetComponent<TextMeshProUGUI>();
        totalBidText.text = currentRoundTotalBid.ToString();

        // Deal the same number of cards as there are players
        for(int i = 0; i < PLAYER_NUM; i++)
        {
            GameObject newPlaceCardPrefab = Instantiate(placeCardPrefab, cardContainer.transform) as GameObject;
            // This means for round 0, list elements 0,1,2,3 are used...
            // And for round 2, elements 8,9,10,11 are used
            int relevantPlaceCardSO = PLAYER_NUM * currentRoundNum + i;

            TextMeshProUGUI title = newPlaceCardPrefab.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI value = newPlaceCardPrefab.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI description = newPlaceCardPrefab.transform.GetChild(3).GetComponent<TextMeshProUGUI>();

            title.text = shuffledPlaceCards[relevantPlaceCardSO].placeCardTitle;
            value.text = shuffledPlaceCards[relevantPlaceCardSO].initialAssessedValue.ToString();
            description.text = shuffledPlaceCards[relevantPlaceCardSO].placeCardDesc;

            dealtPlaceCards.Add(newPlaceCardPrefab);
        }

        //TODO remove this after testing
        // string inputText = dealtPlaceCards[2].transform.GetChild(2).GetComponent<TextMeshProUGUI>().text;
        // int testValue = int.Parse(inputText);

        // Numerically order the
        dealtPlaceCards = dealtPlaceCards.OrderBy(x => int.Parse(x.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text)).ToList();

        // for (int i = 0; i < 4; i++)
        // {
        //     Debug.Log(dealtPlaceCards[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>().text);
        // }
        
        //StartBiddingLoop(dealtPlaceCards);

        for (int i = 0; i < playerList.Count(); i++)
        {
            playerList[i].gotCardInRound = false;
        }
        
        int biddingLoopNum = 0;
        // Bid until there are no cards left
        while (dealtPlaceCards.Count > 0)
        {
            //How many rounds of bidding there has been. Not rounds of dealt cards.
            biddingLoopNum++;
            yield return StartCoroutine(StartBiddingLoop(dealtPlaceCards, biddingLoopNum));
        }

        currentRoundNum++;
    }

    private IEnumerator StartBiddingLoop(List<GameObject> dealtPlaceCards, int biddingLoopNum)
    {
        //TODO change the order of this per round

        if (playerList[0].gotCardInRound != true)
        {
            yield return StartCoroutine(GetPlayerBid(dealtPlaceCards));
        }

        for (int i = 1; i < PLAYER_NUM; i++)
        {
            // Players can only bid on cards if they havent already got one in this round
            if (playerList[i].gotCardInRound != true)
            {
                GetAIBid(playerList[i], i, dealtPlaceCards, biddingLoopNum);   
            }
        }
    }

    private void GetAIBid(PlayerStatSO AIPlayer, int playerID, List<GameObject> dealtPlaceCards, int biddingLoopNum)
    {
        //! AI bid is determined by:
        //! -  existing card values in its deck
        //! -  current money (not currently implemented, maybe tie it to other ai and player money amounts)
        //! -  next most valuable card?

        int bidChance = AI_BID_CHANCE;
        // If the AI bids more then 1000
        int aiBidIncreaseChance = 0;
        int aiBidValue = 1000;
        
        // If there is only 1 card left don't bother raising
        if (dealtPlaceCards.Count > 1)
        {
            int existingCardValues = 0;
            //TODO remove bidIncreaseChance and just have bidChance values over 100% contribute to increasing the bid
            // Debug.Log("Owned Card Count: " + AIPlayer.ownedPlaceCards.Count()); 
            for (int i = 0; i < AIPlayer.ownedPlaceCards.Count(); i++)
            {
                //! Depending on the round, if the AI has a lower than expected card value it may want to bid more aggressively
                existingCardValues += int.Parse(AIPlayer.ownedPlaceCards[i].transform.GetChild(2).GetComponent<TextMeshProUGUI>().text);
            }
            // If the AI got a card worth 4 or less in the first round they will be more aggressive in the current round.
            if (currentRoundNum == 2 && existingCardValues <= 6) { bidChance += 10; aiBidIncreaseChance += 25; } 
            if (currentRoundNum == 3 && existingCardValues <= 9) { bidChance += 15; aiBidIncreaseChance += 30; } 
            if (currentRoundNum == 4 && existingCardValues <= 12) { bidChance += 25; aiBidIncreaseChance += 50; }

            int currentAIMoney = AIPlayer.money;
            if (currentRoundNum == 3 && currentAIMoney > 16000) { bidChance += 60; aiBidIncreaseChance += 30; } 
            if (currentRoundNum == 4 && currentAIMoney > 12000) { bidChance += 80; aiBidIncreaseChance += 50; } 

            // have the bid chance start high, but decrease per bidding round.	
            if (biddingLoopNum == 2) { bidChance += 25; }
            if (biddingLoopNum == 2) { bidChance += 10; }
            if (biddingLoopNum == 3) { bidChance -= 5; }
            if (biddingLoopNum == 3) { bidChance -= 20; }
            if (biddingLoopNum == 3) { bidChance -= 50; }

            GameObject withdrawCard = dealtPlaceCards[0];
            GameObject nextUpCard = dealtPlaceCards[1];
            GameObject lastCard =  dealtPlaceCards.Last();

            int withdrawCardValue = int.Parse(withdrawCard.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text);
            int nextUpCardValue = int.Parse(nextUpCard.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text);
            int highestValue = int.Parse(lastCard.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text);
            // The difference between bowing out now, or keeping bidding for the next card
            // Above a certain amount, the AI will keep bidding as they lose out more by not keeping going
            int nextUpDifference = nextUpCardValue - withdrawCardValue; 

            if (nextUpDifference <= 1) { bidChance -= 50; aiBidIncreaseChance -= 35; }
            if (nextUpDifference <= 2) { bidChance -= 20; aiBidIncreaseChance -= 15; }
            // If it is one of the highest value cards
            if (highestValue > 14) { bidChance += 35; aiBidIncreaseChance += 50;}

            // Cap bid chance to 90%
            if (bidChance > 100) { bidChance = 90; }
            else if (bidChance < 0) { bidChance = 0; }
            if (aiBidIncreaseChance < 0) { aiBidIncreaseChance = 0; }

        } else
        {
            // Do not bid if there is only 1 card left
            bidChance = 0;
        }

        // If the AI has no money they also have to withdraw
        if (Random.Range(1.0f, 100.0f) > bidChance || AIPlayer.money == 0)
        {
            // If the the card taken wasn't the last one, refund half the cost 
            if (dealtPlaceCards.Count() > 1) 
            { 
                // Need to make sure refund amount 
                int refundAmount = playerTotalBuyIns[playerID] / 2;
                int remainder = refundAmount % 1000;
                refundAmount -= remainder;
                AIPlayer.money += refundAmount;

                // Update UI to show refunded amount
                TextMeshProUGUI currentMoney = playerInfo[playerID].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                currentMoney.text = AIPlayer.money.ToString();
            }

            // AI withdraws
            AIPlayer.ownedPlaceCards.Add(dealtPlaceCards[0]);
            dealtPlaceCards[0].transform.SetParent(aiCardContainer.transform);
            dealtPlaceCards[0].SetActive(false);
            dealtPlaceCards.RemoveAt(0);

            AIPlayer.gotCardInRound = true;
            Debug.Log("AI withdrew");
        } else
        {
            Debug.Log("AI raised the bid");

            // Chance to increase bid amount based on some factors
            int secondaryIncreaseChance = aiBidIncreaseChance / 5;
            if (Random.Range(0.0f, 100.0f) < aiBidIncreaseChance) { aiBidValue *= 2; }
            if (Random.Range(0.0f, 100.0f) < secondaryIncreaseChance) { aiBidValue *= 2; }

            // Don't allow AI to spend more than they have
            if (aiBidValue > AIPlayer.money) { aiBidValue = AIPlayer.money; }

            currentRoundTotalBid += aiBidValue;
            AIPlayer.money -= aiBidValue;

            TextMeshProUGUI totalBidText = currentRoundTotalBidText.GetComponent<TextMeshProUGUI>();
            totalBidText.text = currentRoundTotalBid.ToString();

            //Change AIPlayer to get a player by ID?
            TextMeshProUGUI currentMoney = playerInfo[playerID].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            currentMoney.text = AIPlayer.money.ToString();

            // Track how much money was spent so half can be refunded if not taking the final card.
            playerTotalBuyIns[playerID] += aiBidValue;
        }
    }

    private IEnumerator GetPlayerBid(List<GameObject> dealtPlaceCards)
    {
        if ( dealtPlaceCards.Count() != 1)
        {
            yield return WaitforUserBidInput();
        }

        // If there is only 1 card left then don't allow player to raise.
        if (!userBoughtIn || dealtPlaceCards.Count() == 1) 
        { 
            // If the the card taken wasn't the last one, refund half the cost 
            if (dealtPlaceCards.Count() > 1) 
            { 
                // Need to make sure refund amount 
                int refundAmount = playerTotalBuyIns[0] / 2;
                int remainder = refundAmount % 1000;
                refundAmount -= remainder;
                playerList[0].money += refundAmount;
                Debug.Log("Refunded money to player: " + refundAmount);

                // Update UI to show refunded amount
                TextMeshProUGUI currentMoney = playerInfo[0].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                currentMoney.text = playerList[0].money.ToString();
            }

            playerList[0].ownedPlaceCards.Add(dealtPlaceCards[0]);
            dealtPlaceCards[0].transform.SetParent(playersOwnedCardsContainer.transform );

            int storedCardXPos = -30 - (playerList[0].ownedPlaceCards.Count() * 50);

            dealtPlaceCards[0].transform.localPosition = new Vector3(storedCardXPos, -40, 0);
            dealtPlaceCards.RemoveAt(0);

            //! move each new place card to left by 50
            playerList[0].gotCardInRound = true;
        } else
        {
            currentRoundTotalBid += currentBidValue;
            playerList[0].money -= currentBidValue;

            TextMeshProUGUI totalBidText = currentRoundTotalBidText.GetComponent<TextMeshProUGUI>();
            totalBidText.text = currentRoundTotalBid.ToString();

            TextMeshProUGUI currentMoney = playerInfo[0].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            currentMoney.text = playerList[0].money.ToString();

            TextMeshProUGUI bidValueTextField = biddingUI.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            bidValueTextField.text = "1000";
            currentBidValue = 1000;

            playerTotalBuyIns[0] += currentBidValue;
        }

        bidSent = false;
    }

    private IEnumerator WaitforUserBidInput()
    {
        // Waits until the user inputs
        // yield return null is to make the loop run only ever frame and prevent freezes hopefully
        bool inputRecieved = false;
        while(!inputRecieved)
        {
            if (bidSent == true)
            {
                inputRecieved = true;
            }
            yield return null;
        }
    }

    // buyIn refers to whether they withdraw or keep going. True is if they do send a bid.
    public void sendBid(bool buyIn) { bidSent = true; userBoughtIn = buyIn; }

    public void raiseBid(bool isBidRaised)
    {
        // Probably should be const
        int bidRaiseAmount = 1000;
        if (isBidRaised == false) { bidRaiseAmount *= -1; }

        // Prevents player from bidding more than the money they have, or less than 1000
        if (currentBidValue + bidRaiseAmount < 1000 || currentBidValue + bidRaiseAmount > playerList[0].money) { return; }

        currentBidValue += bidRaiseAmount;

        TextMeshProUGUI bidValueTextField = biddingUI.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        bidValueTextField.text = currentBidValue.ToString();
    }



    private IEnumerator InitSellingRound()
    {
        List<GameObject> dealtMoneyCards = new List<GameObject>();

        // Deal the same number of cards as there are players
        for(int i = 0; i < PLAYER_NUM; i++)
        {
            GameObject newMoneyCardPrefab = Instantiate(moneyCardPrefab, cardContainer.transform) as GameObject;
            // This means for round 0, list elements 0,1,2,3 are used...
            // And for round 2, elements 8,9,10,11 are used
            int relevantMoneyCardSO = PLAYER_NUM * currentRoundNum + i;

            TextMeshProUGUI value = newPlaceCardPrefab.transform.GetChild(2).GetComponent<TextMeshProUGUI>();

            value.text = shuffledPlaceCards[relevantPlaceCardSO].initialAssessedValue.ToString();

            dealtMoneyCards.Add(newMoneyCardPrefab);
        }

        // Numerically order the
        dealtPlaceCards = dealtPlaceCards.OrderBy(x => int.Parse(x.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text)).ToList();
        
        for (int i = 0; i < playerList.Count(); i++)
        {
            playerList[i].gotCardInRound = false;
        }
        
        int biddingLoopNum = 0;
        // Bid until there are no cards left
        while (dealtPlaceCards.Count > 0)
        {
            //How many rounds of bidding there has been. Not rounds of dealt cards.
            biddingLoopNum++;
            yield return StartCoroutine(StartMoneyBiddingLoop(dealtMoneyCards, biddingLoopNum));
        }

        currentRoundNum++;
    }
}
