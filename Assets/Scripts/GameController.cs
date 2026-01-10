using System.Collections;
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
  public Button playAgainButton;

  public Text winnerText;

  #region Public methods
  //HIT begin ****************************************************************************************************
  public void Hit()
  {
    player.Push(deck.Pop());
    if (player.HandValue() > 21)
    {
      hitButton.interactable = false;
      stickButton.interactable = false;
      StartCoroutine(DealersTurn());

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
  public void PlayAgain()
  {
    playAgainButton.interactable = false;

    player.GetComponent<CardStackView>().Clear();
    dealer.GetComponent<CardStackView>().Clear();
    deck.GetComponent<CardStackView>().Clear();
    deck.CreateDeck();
    winnerText.text = "";

    hitButton.interactable = true;
    stickButton.interactable = true;

    dealersFirstCard = -1;
    StartGame();
  }
  #endregion
  //STICK end ****************************************************************************************************

  #region Unity messages
  void Start()
  {
    StartGame();
  }
  #endregion

  void StartGame()
  {
    for (int i = 0; i < 2; i++)
    {
      player.Push(deck.Pop());
      HitDealer();
    }
  }

  //HitDealer begin ******************************************************************************************
  public void HitDealer()
  {
    int card = deck.Pop();

    if (dealersFirstCard < 0)
    {
      dealersFirstCard = card;
    }

    dealer.Push(card);
    if (dealer.CardCount >= 2)
    {
      CardStackView view = dealer.GetComponent<CardStackView>();
      view.Toggle(card, true);
    }
  }
  //HitDealer end ******************************************************************************************

  IEnumerator DealersTurn()
  {
    stickButton.interactable = false;
    stickButton.interactable = false;

    CardStackView view = dealer.GetComponent<CardStackView>();
    view.Toggle(dealersFirstCard, true);
    view.ShowCards();
    //yield return new WaitForSeconds(1f);
    HitDealer();

    while (dealer.HandValue() < 17)
    {
      HitDealer();
      yield return new WaitForSeconds(1f);
    }

    if (player.HandValue() > 21 || (dealer.HandValue() >= player.HandValue() && dealer.HandValue() <= 21))
    {
      winnerText.text = "You Loose";
    }
    else if (dealer.HandValue() > 21 || (player.HandValue() <= 21 && player.HandValue() > dealer.HandValue()))
    {
      winnerText.text = "You Won";
    }
    else
    {
      winnerText.text = "The House wins";
    }

    yield return new WaitForSeconds(1f);
    playAgainButton.interactable = true;
  }
}





