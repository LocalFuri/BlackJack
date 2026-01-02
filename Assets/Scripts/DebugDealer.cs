using UnityEngine;

public class DebugDealer : MonoBehaviour
{
  public CardStack dealer;
  public CardStack player;

  public void OnGUI() //Primarily used for debugging, quick prototypes, or editor tools now
  {
    if (GUI.Button(new Rect(10, 10, 256, 28), "Hit me again"))
    {
      player.Push(dealer.Pop());
    }
  }
}
