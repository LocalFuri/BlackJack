using UnityEngine;

public class DebugChangeCard : MonoBehaviour
{
  CardFlipper flipper;//k
  CardModel cardModel;//k
  int cardIndex = 0;//k
  
  public GameObject card;//k

  private void Awake()//k
  {
    cardModel = card.GetComponent<CardModel>();  //k
    flipper   = card.GetComponent<CardFlipper>();//k
  }

  private void OnGUI()
  {
    if(GUI.Button(new Rect(10,10,100,28),"Hit me"))//k
    {
      if (cardIndex >= cardModel.faces.Length) //array 0..51
      {
        cardIndex = 0;//k
        //cardModel.ToggleFace(false); //debugging without CardFlipper
        flipper.FlipCard(cardModel.faces[cardModel.faces.Length - 1], cardModel.cardBack, -1); //k
      }
      else
      {
        //cardModel.cardIndex = cardIndex;  //debugging without CardFlipper
        //cardModel.ToggleFace(true);       //debugging without CardFlipper
        if(cardIndex >0)//k
        {
          flipper.FlipCard(cardModel.faces[cardIndex - 1], cardModel.faces[cardIndex], cardIndex);//k
        }
        else
        {
          flipper.FlipCard(cardModel.cardBack, cardModel.faces[cardIndex], cardIndex);
        }

          cardIndex++;  //only inc if in range
      }

     // Debug.Log(cardIndex);
    }
  }
}
