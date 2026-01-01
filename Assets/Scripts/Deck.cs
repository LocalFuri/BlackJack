using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Deck : MonoBehaviour
{
  List<int> cards;  //k

  public void Shuffle()
  { 
    if (cards == null)
    {
      cards =new List<int>();
    }
    else
    {


    }
    
    void Start()
    { 
    cards = new List<int>();

    }

  }
}
