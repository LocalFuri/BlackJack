using System.Collections.Generic;
using UnityEngine;

public class CardStack : MonoBehaviour
{ //var
  //cards: TList<Integer>;
  List<int> cards;  //k

  public bool isGameDeck;
  public bool HasCards
  {
    get { return cards != null && cards.Count > 0; }
  }

  public event CardEventHandler CardRemoved;
  public event CardEventHandler CardAdded ;

  public int CardCount
  {
    get
    {
      if (cards == null)
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
    foreach (int i in cards)
    {
      yield return i;
    }
  }

  //POP begin **********************************************************************
  public int Pop()
  {
    int temp = cards[0];
    cards.RemoveAt(temp); // Removes item At position (temp)

    if (CardRemoved != null)
    {
      CardRemoved(this, new CardEventArgs(temp));
    }

    return temp;
  }//POP end **********************************************************************

  //Push begin **********************************************************************
  public void Push(int card)
  {
    cards.Add(card);
    if(CardAdded != null)
    {
      CardAdded(this, new CardEventArgs(card));
    }

  }//Push end **********************************************************************

  //HANDVALUE begin **********************************************************************
  public int HandValue() //k
  {
    int total = 0;//k
    int aces = 0;

    foreach (int card in GetCards())//k
    {
      int cardRank = card % 13;// check for naming conventions carRank instead of CardRank
      if (0 == cardRank)
      {
        aces++;
      }
      else if (cardRank < 10)
      {
        cardRank += 1;
      }
      else
      {
        cardRank = 10;
      }
    }
    /* old code

    if (cardRank <= 8) // array[0..8], max =9
    {
      cardRank += 2;//k
      total = total + cardRank;
    }
    else if (cardRank > 8 && cardRank < 12)//k
    {
      cardRank = 10;//k
      total = total + cardRank;
    }
    else
    {
      aces++;
    }

  } */

    //***************************************************************************
    for (int i =  0; i < aces; i++) //k
    {
      if(total + 11 <= 21)//k
      {
        total = total + 11;//k
      }
      else//k
      {
        total = total + 1;//k   
      }
    }
    return total;
  }
  //HANDVALUE end **********************************************************************

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

  public void Reset()
  {
    cards.Clear();
  }

  void Awake()
  {
    Debug.Log("sssssssssssssssssssssssssssssss");
    cards = new List<int>();
    if (isGameDeck)
    {
      CreateDeck();
    }
  }
}
