using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CardStack))] // making your code more robust
public class CardStackView : MonoBehaviour
{
  CardStack deck;//k
  Dictionary<int, GameObject> fetchedCards;
  int lastCount;//k

  public Vector3 start;//can use Vector2
  public float cardOffset;
  public bool faceUp =false;
  public bool reverseLayerorder =false;
  

  public GameObject cardPrefab;//k

  public int cardIndex;
  

  //void START begin ************************************************************************
  void Start()
  {
    fetchedCards = new Dictionary<int, GameObject>();//k
    deck = GetComponent<CardStack>();//k
    ShowCards();//k
    lastCount = deck.CardCount;  //k

    deck.CardRemoved += deck_CardRemoved;//k
  }
  //Deck_CardRemoved begin ----------------------------------------------------------------------------
  private void deck_CardRemoved(object sender, CardRemovedEventArgs e)
  {
    if (fetchedCards.ContainsKey(e.CardIndex))
    {
      Destroy(fetchedCards[e.CardIndex]);
      fetchedCards.Remove(e.CardIndex);
    }
  }
  //Deck_CardRemoved end ----------------------------------------------------------------------------
  //void START end ************************************************************************

  //void UPDATE begin ************************************************************************
  void Update()
  {
    if(lastCount !=deck.CardCount)
    {
      lastCount = deck.CardCount;
      ShowCards();  
    }
  }
  //void UPDATE end ************************************************************************
  
  //SHOWCARDS begin *********************************************************************************
  private void ShowCards()
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
      spriteRenderer.sortingOrder = 51 - positionalIndex;
    }
    else
    {
      spriteRenderer.sortingOrder = positionalIndex;
    }

    spriteRenderer.sortingOrder = positionalIndex;  //1. choice of order

    fetchedCards.Add(CardIndex, cardCopy);
    Debug.Log("Hand Value = " + deck.HandValue());
  }
 }
