using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

[RequireComponent(typeof(Deck))] // making your code more robust
public class DeckView : MonoBehaviour
{
  Deck deck;//k

  public Vector3 start;//k
  public float cardOffset;//k
  public GameObject cardPrefab;//k

  void Start()
  {
    deck = GetComponent<Deck>();//k
    ShowCards();
  }

  private void ShowCards()
  {
    int cardCount = 0;//k
    //cardOffset = 0;

    foreach (int i in deck.GetCards())//k
      {
      float co = cardOffset * cardCount;//k

      //GameObject cardCopy = (GameObject)Instantiate(cardPrefab); //Instantiate =creating copies of GameObjects
      GameObject cardCopy = Instantiate(cardPrefab); //Instantiate =creating copies of GameObjects
      Vector3 temp = start + new Vector3(co, 0f); //you can use Vector2 too
      cardCopy.transform.position = temp;//k

      CardModel cardModel =cardCopy.GetComponent<CardModel>();//k
      cardModel.cardIndex = i;//k 
      cardModel.ToggleFace(true);//k

      SpriteRenderer spriteRenderer = cardCopy.GetComponent<SpriteRenderer>();
      //spriteRenderer.sortingOrder = cardCount;  //1. choice of order
      spriteRenderer.sortingOrder = 51 -cardCount;//2. choice of order

      cardCount++;//k
    }
  }
}
