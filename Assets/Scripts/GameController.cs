using System.Collections;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
  int dealersFirstCard = -1;
  public CardStack player;
  public CardStack dealer;
  public CardStack deck;

  public Button hitButton;
  public Button stickButton;

  #region Public methods
  //HIT begin ****************************************************************************************************
  public void Hit()
  {
    player.Push(deck.Pop());
    if(player.HandValue() > 21)
    {
      hitButton.interactable    = false;
      stickButton.interactable  = false;
    }
  }
  //HIT end ****************************************************************************************************

  //STICK begin ****************************************************************************************************

  public void Stick()
  {
    hitButton.interactable = false;
    stickButton.interactable = false;
    StartCoroutine(DealersTurn());
  }
  #endregion
  //STICK end ****************************************************************************************************

  void StartGame()
  {
    for(int i = 0; i < 2; i++)
    {
      HitDealer();
    }
  }
  //HitDealer begin ******************************************************************************************
  void HitDealer()
  {
    int card = deck.Pop();

    if(dealersFirstCard < 0)
    {
      dealersFirstCard = card;
    }

    dealer.Push(card);
    if(dealer.CardCount >= 2)
    {
      CardStackView view = dealer.GetComponent<CardStackView>();
      view.Toggle(card, true);
    }
  }
  //HitDealer end ******************************************************************************************

  IEnumerator DealersTurn()
  {
    CardStackView view = dealer.GetComponent<CardStackView>();
    view.Toggle(dealersFirstCard, true);
    view.ShowCards();
    yield return new WaitForSeconds(1f);

    while (dealer.HandValue() < 17)
    {
      HitDealer();
      yield return new WaitForSeconds(1f);
    }
  }
}





