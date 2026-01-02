 using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CardStack))] // making your code more robust
public class CardStackView : MonoBehaviour
{
  CardStack deck;//k
  Dictionary<int, GameObject> fetchedCards;
  int lastCount;//k

  public Vector3 start;//can use Vector2
  public float cardOffset;//k
  public GameObject cardPrefab;//k
  private int cardIndex;

  //void START begin ************************************************************************
  void Start()//k 
  {
    fetchedCards = new Dictionary<int, GameObject>();
    deck = GetComponent<CardStack>();
    ShowCards();
    lastCount = deck.CardCount;  

    deck = GetComponent<CardStack>();  //k 
    ShowCards();//k
    lastCount = deck.CardCount;//k
  }
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
  private void ShowCards()//k
  {
    int cardCount = 0;//k
    if (deck.HasCards )//k
    {
      foreach (int i in deck.GetCards())
      {
        float co = cardOffset * cardCount;
        Vector3 temp = start + new Vector3(co, 0f); //you can use Vector2 too
        AddCard(temp, i, cardCount); 
        cardCount++;
      }
    }
  }
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
    cardModel.ToggleFace(true);

    SpriteRenderer spriteRenderer = cardCopy.GetComponent<SpriteRenderer>();
    spriteRenderer.sortingOrder = positionalIndex;  //1. choice of order

    fetchedCards.Add(CardIndex, cardCopy);
    
  }
 }
