using Assets.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CardStack))] // making your code more robust
public class CardStackView : MonoBehaviour
{
  CardStack deck;
  Dictionary<int, CardView> fetchedCards;
  int lastCount;

  public Vector3 start;//can use Vector2
  public float cardOffset;
  public bool faceUp =false;
  public bool reverseLayerorder =false;
  public GameObject cardPrefab;

  public void Toggle(int card, bool isFaceUp)
  {
    fetchedCards[card].IsFaceUp = isFaceUp;
  }

  public int cardIndex;
 
  //void START begin ************************************************************************
  void Awake()
  {
    fetchedCards = new Dictionary<int, CardView>();
    deck = GetComponent<CardStack>();
    ShowCards();
    lastCount = deck.CardCount;

    deck.CardRemoved  += deck_CardRemoved;
    deck.CardAdded    += deck_CardAdded ;
  }

  //Deck_CardAdded begin ----------------------------------------------------------------------------
  void deck_CardAdded(object sender, CardEventArgs e)
  {
    float co = cardOffset * deck.CardCount;
    Vector3 temp =start + new Vector3(co, 0f);
    AddCard(temp, e.CardIndex, deck.CardCount);

  }
  //Deck_CardAdded end ----------------------------------------------------------------------------  

  //Deck_CardRemoved begin ----------------------------------------------------------------------------
  private void deck_CardRemoved(object sender, CardEventArgs e)
  {
    if (fetchedCards.ContainsKey(e.CardIndex))
    {
      Destroy(fetchedCards[e.CardIndex].Card);
      fetchedCards.Remove(e.CardIndex);
    }
  }
  //Deck_CardRemoved end ----------------------------------------------------------------------------
  //void START end ************************************************************************

  //void UPDATE begin ************************************************************************
  void Update()
  {
    if (lastCount != deck.CardCount)
    {
      lastCount = deck.CardCount;
      ShowCards();
    }
  }
  //void UPDATE end ************************************************************************
  
  //SHOWCARDS begin *********************************************************************************
  public void ShowCards()
  {
    int cardCount = 0;
    if (deck.HasCards )
    {
      foreach (int i in deck.GetCards())
      {
        float co = cardOffset * cardCount;
        Vector3 temp = start + new Vector3(co, 0f); //you can use Vector2 too
        AddCard(temp, i, cardCount); 
        cardCount++;
      }
    }
  }//SHOWCARDS end *********************************************************************************

  void AddCard(Vector3 position, int CardIndex, int positionalIndex)//k
  {
    if (fetchedCards.ContainsKey(CardIndex))
    {
      if (!faceUp)
      {
        CardModel model = fetchedCards[cardIndex].Card.GetComponent<CardModel>();
        model.ToggleFace(fetchedCards[CardIndex].IsFaceUp);
      }
      return;
    }

    GameObject cardCopy = (GameObject)Instantiate(cardPrefab); //Instantiate =creating copies of GameObjects
    cardCopy.transform.position = position; //k

    CardModel cardModel = cardCopy.GetComponent<CardModel>();//k
    cardModel.cardIndex = cardIndex;
    cardModel.ToggleFace(faceUp);

    SpriteRenderer spriteRenderer = cardCopy.GetComponent<SpriteRenderer>();
    if (reverseLayerorder)
    {
      spriteRenderer.sortingOrder = positionalIndex;//1. choice of order
    }
    else
    {
      spriteRenderer.sortingOrder = 51 - positionalIndex;
    }

    fetchedCards.Add(CardIndex, new CardView(cardCopy));
    Debug.Log("Hand Value = " + deck.HandValue());
  }

}
