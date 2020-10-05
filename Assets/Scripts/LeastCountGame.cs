using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity;
using UnityEngine.UI;

using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun;
using System;
using TMPro;


namespace QGAMES
{
    public class LeastCountGame : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public TextMeshProUGUI MessageText;

        public TextMeshProUGUI DroporDrawDeckText;

        public TextMeshProUGUI ShoworDrawDroppedText;

        CardAnimator cardAnimator;

        public LeastCountManager leastCountManager;

        public GameObject p1Im;
        public GameObject p2Im;
        public GameObject p3Im;
        public GameObject p4Im;
        public GameObject p5Im;
        public GameObject p6Im;


        public List<Transform> PlayerPositions = new List<Transform>();
        public List<Transform> BookPositions = new List<Transform>();

        MyPlayer localPlayer;
        MyPlayer remotePlayer;

        Dictionary<int, MyPlayer> MyPlayers = new Dictionary<int, MyPlayer>();

        MyPlayer winner;

        MyPlayer currentTurnPlayer;

        List<Card> selectedCards = new List<Card>();
        Card selectedCard;
        Ranks selectedRank;
        Ranks deckOrDroppedCard;
        PlayerMove move;
        bool intializing = false;

        public enum GameState
        {
            Idle,
            GameStarted,
            TurnStarted,
            TurnSelectingDroppingCard,
            TurnConfirmDroppingCard,
            TurnDrawingCard,
            TurnDrawingCardConfirmed,
            Show,
            GameFinished
        };

        public override void OnEnable()
        {
            base.OnEnable();

            CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
        }

        public override void OnDisable()
        {
            base.OnDisable();

            CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;
        }

        public GameState gameState = GameState.Idle;

        private void Awake()
        {

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object isPlayerName;
                object avatar;
                p.CustomProperties.TryGetValue(Constants.PLAYER_NAME, out isPlayerName);
                p.CustomProperties.TryGetValue(Constants.PLAYER_AVATAR, out avatar);
                MyPlayer aplayer = new MyPlayer();
                aplayer.PlayerId = p.ActorNumber.ToString();
                aplayer.PlayerName = isPlayerName.ToString();                
                Debug.Log("Player p1" + p.ActorNumber);                
                int pos = CaluclatePositions(p.ActorNumber, PhotonNetwork.LocalPlayer.ActorNumber);
                if (pos == 0)
                {
                    aplayer.IsLocalPlayer = true;
                    localPlayer = aplayer;
                }
                ShowJoinedPlayer(pos+1,(string) avatar);
                aplayer.Position = PlayerPositions[pos].position;
                MyPlayers.Add(p.ActorNumber, aplayer);
                Debug.Log("aplaer position" + aplayer.Position);
            }
            leastCountManager = new LeastCountManager(MyPlayers);
            cardAnimator = FindObjectOfType<CardAnimator>();
        }

        void Start()
        {
            gameState = GameState.GameStarted;
            GameFlow();
        }

        //****************** Game Flow *********************//
        public void GameFlow()
        {
            if (gameState > GameState.GameStarted)
            {
                CheckPlayersBooks();
                ShowAndHidePlayersDisplayingCards();
                SetButtonsText();
            }

            switch (gameState)
            {
                case GameState.Idle:
                    {
                        Debug.Log("IDEL");
                        break;
                    }
                case GameState.GameStarted:
                    {
                        Debug.Log("GameStarted");
                        OnGameStarted();
                        break;
                    }
                case GameState.TurnStarted:
                    {
                        Debug.Log("TurnStarted");
                        OnTurnStarted();
                        break;
                    }
                case GameState.TurnSelectingDroppingCard:
                    {
                        Debug.Log("TurnSelectingNumber");
                        OnTurnSelectingDroppingCard();
                        break;
                    }
                case GameState.TurnConfirmDroppingCard:
                    {
                        Debug.Log("TurnComfirmedSelectedNumber");
                        OnTurnConfirmDroppingCard();
                        break;
                    }
                case GameState.TurnDrawingCard:
                    {
                        Debug.Log("TurnWaitingForOpponentConfirmation");
                        OnTurnDrawingCard();
                        break;
                    }
                case GameState.TurnDrawingCardConfirmed:
                    {
                        Debug.Log("TurnOpponentConfirmed");
                        OnTurnDrawingCardConfirmed();
                        break;
                    }
                case GameState.Show:
                    {
                        Debug.Log("TurnGoFish");
                        OnShow();
                        break;
                    }
                case GameState.GameFinished:
                    {
                        Debug.Log("GameFinished");
                        OnGameFinished();
                        break;
                    }
            }
        }

        void OnGameStarted()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                leastCountManager.Shuffle();
                Dictionary<string, byte[]> dict = new Dictionary<string, byte[]>();

                //distribute cards to players
                foreach (MyPlayer oplayer in MyPlayers.Values)
                {
                    List<byte> playerValues = leastCountManager.DealCardValuesToPlayer(oplayer, Constants.PLAYER_INITIAL_CARDS);
                    dict.Add(oplayer.PlayerId, playerValues.ToArray());
                    if (oplayer.IsLocalPlayer)
                    {
                        currentTurnPlayer = oplayer;
                    }
                }

                List<byte> poolOfCards = leastCountManager.GetPoolOfCards();
                byte droppedCardValue = leastCountManager.FirstDroppedCard();
                List<byte> droppedListValue = new List<byte>();
                droppedListValue.Add(droppedCardValue);
                dict.Add("poolOfCards", poolOfCards.ToArray());
                dict.Add(Constants.INITIALIZING_DROPPEDCARD, droppedListValue.ToArray());

                byte evCode = Constants.SHUFFLE_EVCODE; // Custom Event 1: Used as "MoveUnitsToTargetPosition" event
                //object[] content = new object[] { dict }; // Array contains the target position and the IDs of the selected units
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
                PhotonNetwork.RaiseEvent(evCode, dict, raiseEventOptions, SendOptions.SendReliable);
                Debug.Log("master onstarted");

                foreach (MyPlayer oplayer in MyPlayers.Values)
                {
                    if (!oplayer.IsLocalPlayer)
                    {
                        cardAnimator.DealDisplayingCards(oplayer, Constants.PLAYER_INITIAL_CARDS);
                    }
                    else
                    {
                        cardAnimator.DealDisplayingCardsToLocalPlayer(oplayer, Constants.PLAYER_INITIAL_CARDS);
                    }
                }
                Card firstDroppedCard = cardAnimator.DropFirstCard(droppedCardValue);
                leastCountManager.AddToDropCardsReference(firstDroppedCard);

            }
            else
            {
                if (intializing && !PhotonNetwork.IsMasterClient)
                {
                    foreach (MyPlayer oplayer in MyPlayers.Values)
                    {
                        if (!oplayer.IsLocalPlayer)
                        {
                            cardAnimator.DealDisplayingCards(oplayer, Constants.PLAYER_INITIAL_CARDS);
                        }
                        else
                        {
                            cardAnimator.DealDisplayingCardsToLocalPlayer(oplayer, Constants.PLAYER_INITIAL_CARDS);
                        }
                    }
                    Card firstDroppedCard = cardAnimator.DropFirstCard(leastCountManager.GetDroppedCardValues()[0]);
                    leastCountManager.AddToDropCardsReference(firstDroppedCard);
                    intializing = false;

                }
            }

        }

        void OnTurnStarted()
        {
            SwitchTurn();
            gameState = GameState.TurnSelectingDroppingCard;
            GameFlow();
        }

        public void OnTurnSelectingDroppingCard()
        {

            ResetSelectedCard();
            move = new PlayerMove();

            if (currentTurnPlayer == localPlayer)
            {
                SetMessage($"Your turn. Pick a card from your hand.select a card");
            }
            else
            {
                //SetMessage($"{currentTurnPlayer.PlayerName}'s turn");
                SetMessage("currentTurnPlayer.PlayerName's turn");
            }

        }

        public void OnTurnConfirmDroppingCard()
        {
            Dictionary<string, byte> dict3 = new Dictionary<string, byte>();
            dict3.Add("CurrentActorNumber", Convert.ToByte(PhotonNetwork.LocalPlayer.ActorNumber));
            string dropString = "dropString";
            for(int i=0;i<selectedCards.Count;i++) {
                dict3.Add(dropString + i.ToString(),selectedCards[i].Value);
            }
            byte evCode = Constants.DROP_EVCODE;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, dict3, raiseEventOptions, SendOptions.SendReliable);

            gameState = GameState.TurnDrawingCard;
            GameFlow();

            SetMessage($" {currentTurnPlayer.PlayerName}dropped {selectedRank},click draw from deck or draw from dropped buttons");
        }

        public void OnTurnDrawingCard()
        {

        }

        public void OnTurnDrawingCardConfirmed()
        {
            move.CurrentActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            move.NextActorNumber = PhotonNetwork.LocalPlayer.GetNext().ActorNumber;
            Dictionary<string, byte> dict2 = new Dictionary<string, byte>();
            dict2.Add("droppedCards", move.droppedCards);
            dict2.Add("drawnCard", move.drawnCard);
            dict2.Add("CurrentActorNumber", Convert.ToByte(move.CurrentActorNumber));
            dict2.Add("NextActorNumber", Convert.ToByte(move.NextActorNumber));
            if (move.drawnFromDeckOrDropped == "dropped")
            {
                dict2.Add("drawnFromDeckOrDropped", 0);
            }
            else
            {
                dict2.Add("drawnFromDeckOrDropped", 1);

            }

            byte evCode = Constants.DRAW_EVCODE;
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All }; // You would have to set the Receivers to All in order to receive this event on the local client as well
            PhotonNetwork.RaiseEvent(evCode, dict2, raiseEventOptions, SendOptions.SendReliable);

        }

        public void OnShow()
        {
            gameState = GameState.GameFinished;
            GameFlow();
        }


        public void OnGameFinished()
        {
            //comehere last 
            if (MyPlayers[leastCountManager.Winner()] == localPlayer)
            {
                SetMessage($"You WON!");
            }
            else
            {
                SetMessage($"You LOST!");
            }
        }

        //****************** Helper Methods *********************//
        public void ResetSelectedCard()
        {
            if (selectedCard != null)
            {
                selectedCard.OnSelected(false);
                selectedCard = null;
                selectedRank = 0;
            }
        }

        void SetMessage(string message)
        {
            MessageText.text = message;
        }

        public void SwitchTurn()
        {
            //if (currentTurnPlayer == null)
            //{
            //    currentTurnPlayer = localPlayer;
            //    return;
            //}           
        }

        void ShowJoinedPlayer(int pNo,string avatar) {
            string avName = "avatars/avatars0" + avatar;
            Debug.Log("avName" + avName);
            if (pNo == 1) {
                p1Im.GetComponent<Image>().sprite = Resources.Load<Sprite>(avName);
            }
            else if (pNo == 2)
            {
                Image i = p2Im.GetComponent<Image>();
                i.sprite = Resources.Load<Sprite>(avName);                
            }
            else if (pNo == 3)
            {
                p3Im.GetComponent<Image>().sprite = Resources.Load<Sprite>(avName);
            }
            else if (pNo == 4)
            {
                p4Im.GetComponent<Image>().sprite = Resources.Load<Sprite>(avName);
            }
            else if (pNo == 5)
            {
                p5Im.GetComponent<Image>().sprite = Resources.Load<Sprite>(avName);
            }
            else if (pNo == 6)
            {
                p6Im.GetComponent<Image>().sprite = Resources.Load<Sprite>(avName);
            }

        }
        public void CheckPlayersBooks()
        {
            foreach (MyPlayer aplayer in MyPlayers.Values)
            {
                List<byte> playerCardValues = leastCountManager.PlayerCards(aplayer);
                aplayer.SetCardValues(playerCardValues);
            }
        }


        public void ShowAndHidePlayersDisplayingCards()
        {
            foreach (MyPlayer aplayer in MyPlayers.Values)
            {
                if (aplayer.IsLocalPlayer)
                {
                    aplayer.ShowCardValues();
                }
                else
                {
                    aplayer.HideCardValues();
                }

            }
        }

        public void SetButtonsText()
        {
            if (gameState < GameState.TurnConfirmDroppingCard)
            {
                ShoworDrawDroppedText.text = "Show";
                DroporDrawDeckText.text = "Drop Card";
            }
            else
            {
                ShoworDrawDroppedText.text = "Draw Dropped Cards";
                DroporDrawDeckText.text = "Draw from Deck";
            }

        }

        //****************** User Interaction *********************//
        public void OnCardSelected(Card card)
        {
            Debug.Log("Card selected");
            if (gameState == GameState.TurnSelectingDroppingCard && currentTurnPlayer == localPlayer)
            {
                Debug.Log("Card selected gameState TurnSelectingDroppingCard");
                if (card.OwnerId == currentTurnPlayer.PlayerId)
                {
                    if (ConditionsForCardSelection(card))
                    {
                        selectedCard = card;
                        card.OnSelected(true);
                        selectedCards.Add(card);
                        selectedRank = selectedCard.Rank;
                    }
                    SetMessage($"{currentTurnPlayer.PlayerName} ,do you want to drop  {selectedCard.Rank} ?");
                }
            }
        }

        public bool ConditionsForCardSelection(Card card)
        {
            if (selectedCards.Count == 0)
            {
                return true;
            }
            else if (selectedCards.Contains(card))
            {
                return false;
            }
            else if (card.Rank == selectedCards[0].Rank)
            {
                return true;
            }

            else
            {
                foreach (Card c in selectedCards)
                {
                    c.OnSelected(false);
                }
                selectedCards.Clear();
                return true;
            }

        }


        public void OnShowButton()
        {
            winner = MyPlayers[leastCountManager.Winner()];
            SetMessage($" {winner.PlayerName} Won the game ");
            gameState = GameState.Show;
            GameFlow();

        }

        public void OnDrawFromDeckButton()
        {
            if (gameState == GameState.TurnConfirmDroppingCard)
            {
                byte cardValue = leastCountManager.DrawCardValue();

                if (cardValue == Constants.POOL_IS_EMPTY)
                {
                    Debug.LogError("Pool is empty");
                    return;
                }

                cardAnimator.DrawDisplayingCard(currentTurnPlayer, cardValue);
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, cardValue);
                move.drawnFromDeckOrDropped = "deck";
                move.drawnCard = cardValue;
                gameState = GameState.TurnDrawingCardConfirmed;
                GameFlow();
            }
            else
            {
                SetMessage("Drop the card and click on confirm card button first");
            }
        }

        public void OnDrawFromLastDroppedButton()
        {
            if (gameState == GameState.TurnConfirmDroppingCard)
            {
                Card card = leastCountManager.DrawDroppedCard();
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, card.GetValue());
                cardAnimator.DrawDroppedCard(currentTurnPlayer, card);
                leastCountManager.RepositionDroppedCards(cardAnimator);
                move.drawnFromDeckOrDropped = "dropped";
                move.drawnCard = card.GetValue();
                gameState = GameState.TurnDrawingCardConfirmed;
                GameFlow();
            }
            else
            {
                SetMessage("Drop the card and click on confirm card button first");
            }

        }

        //to drop card
        public void ConfirmDropButton()
        {
            Debug.Log("Drop button clicked");
            if (selectedCard != null || selectedCards != null)
            {
                foreach (Card selCard in selectedCards)
                {
                    leastCountManager.DropCardsFromPlayer(currentTurnPlayer, selCard);
                    Debug.Log("Card Player" + currentTurnPlayer + "," + selCard);
                    cardAnimator.DropCardAnimation(selectedCard, leastCountManager.GetDroppedCardsCount());
                    currentTurnPlayer.DropCardFromPlayer(cardAnimator, selectedCard.GetValue(), true);
                }
                leastCountManager.RepositionDroppedCards(cardAnimator);
                move.droppedCards = selectedCard.GetValue();
                gameState = GameState.TurnConfirmDroppingCard;
                Debug.Log("Card Droped");
                Debug.Log("Card Droped selectedCard" + selectedCard);
                Debug.Log("Card Droped selectedCard GetDroppedCardsCount" + leastCountManager.GetDroppedCardsCount());
                GameFlow();

            }
            else
            {
                SetMessage("Select a card from your deck and click confirm");
            }
        }

        //****************** Animator Event *********************//
        public void MoveAnimations(byte value, byte deckOrDrawn, byte cardValue)
        {
            leastCountManager.DropCardsFromPlayer(remotePlayer, remotePlayer.DisplayingCards[0]);
            cardAnimator.DropCardAnimation(remotePlayer.DisplayingCards[0], leastCountManager.GetDroppedCardsCount());
            currentTurnPlayer.DropCardFromPlayer(cardAnimator, value, true);
            leastCountManager.RepositionDroppedCards(cardAnimator);
            if (deckOrDrawn == 0)
            {
                Card card = leastCountManager.DrawDroppedCard();
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, card.GetValue());
                cardAnimator.DrawDroppedCard(currentTurnPlayer, card);
                leastCountManager.RepositionDroppedCards(cardAnimator);
            }
            else
            {
                cardAnimator.DrawDisplayingCard(currentTurnPlayer, cardValue);
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, cardValue);
            }

        }

        public void DroppedAnimations( byte value) {
            
                Card returnedCard = currentTurnPlayer.DropCardFromPlayer(cardAnimator, value, false);
                leastCountManager.DropCardsFromPlayer(currentTurnPlayer, returnedCard);
                cardAnimator.DropCardAnimation(returnedCard, leastCountManager.GetDroppedCardsCount());
                leastCountManager.RepositionDroppedCards(cardAnimator);
            
        }

        public void DrawCardAnimations(byte deckOrDrawn, byte cardValue) {
            if (deckOrDrawn == 0)
            {
                Card card = leastCountManager.DrawDroppedCard();
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, card.GetValue());
                cardAnimator.DrawDroppedCard(currentTurnPlayer, card);
                leastCountManager.RepositionDroppedCards(cardAnimator);
            }
            else
            {
                cardAnimator.DrawDisplayingCard(currentTurnPlayer, cardValue);
                leastCountManager.AddCardValueToPlayer(currentTurnPlayer.PlayerId, cardValue);
            }
        }
        //****************** Animator Event *********************//
        public void AllAnimationsFinished()
        {
            if (gameState == GameState.GameStarted)
            {
                gameState = GameState.TurnStarted;
                GameFlow();
            }
        }

        private void OnCountdownTimerIsExpired()
        {
            //StartGame();
        }
        //*********************Call Backs *******************//
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey(Constants.INITIALIZING_CARDS))
            {

                Debug.Log("Intializing cards");
                if (!PhotonNetwork.IsMasterClient)
                {
                    Dictionary<string, List<byte>> reply = (Dictionary<string, List<byte>>)propertiesThatChanged[Constants.INITIALIZING_CARDS];

                    if (reply.ContainsKey(localPlayer.PlayerId))
                    {

                        leastCountManager.AddCardValuesToPlayer(localPlayer.PlayerId, reply[localPlayer.PlayerId]);
                        leastCountManager.AddCardValuesToPlayer(remotePlayer.PlayerId, reply[remotePlayer.PlayerId]);
                        leastCountManager.SetPoolOfCards(reply["poolOfCards"]);
                        leastCountManager.AddCardToDroppedCards(reply[Constants.INITIALIZING_DROPPEDCARD][0]);
                        intializing = true;
                        gameState = GameState.GameStarted;
                        GameFlow();
                    }
                }
            }
            if (propertiesThatChanged.ContainsKey(Constants.GAME_STATE_CHANGED))
            {
                int state = (int)propertiesThatChanged[Constants.GAME_STATE_CHANGED];
                if (!PhotonNetwork.IsMasterClient)
                {
                    CheckPlayersBooks();
                    ShowAndHidePlayersDisplayingCards();

                }
            }
            if (propertiesThatChanged.ContainsKey(Constants.PLAYER_MOVE))
            {
                PlayerMove move = (PlayerMove)propertiesThatChanged[Constants.PLAYER_MOVE];
                int justPlayed = move.CurrentActorNumber;
                byte replyDroppedCards = move.droppedCards;
                byte drawnCard = move.drawnCard;
                string drawn = move.drawnFromDeckOrDropped;
                int currentPlayerId = move.NextActorNumber;
                if (justPlayed != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    MoveAnimations(replyDroppedCards, 0, drawnCard);
                }

                if (currentPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    currentTurnPlayer = localPlayer;
                    gameState = GameState.TurnSelectingDroppingCard;
                    GameFlow();

                }
                //Animation

            }
        }

        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;

            //Event code 1-- initial distribution of cards

            if (eventCode == Constants.SHUFFLE_EVCODE)
            {
                if (!PhotonNetwork.IsMasterClient)
                {

                    Debug.Log(photonEvent.CustomData.GetType());
                    Dictionary<string, byte[]> reply = (Dictionary<string, byte[]>)photonEvent.CustomData;

                    //Dictionary<string, List<byte>> reply = data;

                    if (reply.ContainsKey(localPlayer.PlayerId))
                    {
                        foreach (int keys in MyPlayers.Keys)
                        {
                            leastCountManager.AddCardValuesToPlayer(keys.ToString(), reply[keys.ToString()].ToList());
                        }
                        leastCountManager.SetPoolOfCards((List<byte>)reply["poolOfCards"].ToList());
                        leastCountManager.AddCardToDroppedCards(reply[Constants.INITIALIZING_DROPPEDCARD][0]);
                        intializing = true;
                        currentTurnPlayer = remotePlayer;
                        gameState = GameState.GameStarted;
                        GameFlow();
                    }
                }
            }
            else if (eventCode == Constants.DRAW_EVCODE)
            {

                Dictionary<string, byte> move = (Dictionary<string, byte>)photonEvent.CustomData;
                int justPlayed = Convert.ToInt32(move["CurrentActorNumber"]);
                byte replyDroppedCards = move["droppedCards"];
                byte drawnCard = move["drawnCard"];
                byte drawnFromDeckOrDropped = move["drawnFromDeckOrDropped"];
                int currentPlayerId = Convert.ToInt32(move["NextActorNumber"]);
                if (justPlayed != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    MoveAnimations(replyDroppedCards, drawnFromDeckOrDropped, drawnCard);
                }

                if (currentPlayerId == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    currentTurnPlayer = localPlayer;
                    gameState = GameState.TurnSelectingDroppingCard;
                    GameFlow();

                }
                else
                {
                    currentTurnPlayer = remotePlayer;
                    gameState = GameState.TurnSelectingDroppingCard;
                    GameFlow();
                }
                
            }
            else if (eventCode == Constants.DROP_EVCODE) {
                Dictionary<string, byte> move = (Dictionary<string, byte>)photonEvent.CustomData;
                int justPlayed = Convert.ToInt32(move["CurrentActorNumber"]);
                if (justPlayed != PhotonNetwork.LocalPlayer.ActorNumber) {
                    var keyList = new List<string>(move.Keys);
                    for (int i = 0; i < keyList.Count; i++)
                    {
                        var key = keyList[i];
                        if (key.Contains("dropString")) {
                            DroppedAnimations(move[key]);
                        }
                    }
                }
            }

        }

        int CaluclatePositions(int actorNo, int localPlayerNo)
        {
            if (actorNo - localPlayerNo < 0)
            {
                int k = actorNo - localPlayerNo;
                return 6 + k;
            }
            else if (actorNo - localPlayerNo > 0)
            {
                return actorNo - localPlayerNo;
            }
            else
            {
                return 0;
            }
        }
    }
}
