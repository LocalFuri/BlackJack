using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CardStack : MonoBehaviour
{ //var
  //cards: TList<Integer>;
  List<int> cards;  //k

  public bool isGameDeck;
  public bool HasCards
  {
    get { return cards != null && cards.Count > 0; }
  }

  public int CardCount
  {
    get
    {
      if(cards == null)
      {
        return 0;
      }
      else
      {
        return cards.Count;
      }
    }
  }
  
  public IEnumerable<int> GetCards()
  {
    foreach(int i in cards)
    {
      yield return i;
    }
  }

  //Pop begin **********************************************************************
  public int Pop()
  {
    int temp =cards[0];   
    cards.RemoveAt(temp); // Removes item At position (temp)
    return temp;
  }
  //Pop end **********************************************************************

  //Push begin **********************************************************************
  public void Push(int card)
  {
    cards.Add(card);
  }
  //Push end **********************************************************************

  public void CreateDeck()
  {
    cards.Clear();  //remove all elements from identifier

    for(int i = 0; i < 52; i++)
    {
      cards.Add(i);
    }

    //shuffle deck
    int n = cards.Count;
    while (n > 1)
    {
      n--; //dec (i)
      int k =Random.Range(0, n + 1);
      int temp = cards[k];
      cards[k] = cards[n];
      cards[n] = temp;
    }
  }
  void Start()
  {
    cards = new List<int>();
    if (isGameDeck)
    {
      CreateDeck();
    }
  }
}






