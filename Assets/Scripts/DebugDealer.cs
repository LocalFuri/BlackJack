using UnityEngine;
using System.Collections;

public class DebugDealer : MonoBehaviour
{
  public CardStack dealer;
  public CardStack player;

  /*
  // Sloan = future proof of code
  int count = 0;
  int[] cards = new int[] {9, 7, 12};
  */
    
  void OnGUI() //Primarily used for debugging, quick prototypes, or editor tools now
  {
    if (GUI.Button(new Rect(10, 10, 256, 28), "Hit me again"))
    {
      player.Push(dealer.Pop());
    }
  }
}
