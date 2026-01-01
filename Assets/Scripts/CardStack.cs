using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Deck : MonoBehaviour
{
  //var
  //cards: TList<Integer>;
  List<int> cards;  //k

  public IEnumerable<int> GetCards()
  {
    foreach(int i in cards)
    {
      yield return i;
    }
  }


  void Start()
  {
    cards = new List<int>();
    Shuffle(); //call function
  }

  public void Shuffle()
  {
    if (cards == null)
    {
      cards =new List<int>(); //create array
    }
    else
    {
      cards.Clear();  //remove all elements from identifier
    }

    for(int i = 0; i < 52; i++)
    {
      cards.Add(i);
    }

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
}
