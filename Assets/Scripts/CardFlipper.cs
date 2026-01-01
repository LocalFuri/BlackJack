using System.Collections;
//using UnityEditor.Experimental.GraphView; //had to remove it, causes error building
using UnityEngine;

public class CardFlipper : MonoBehaviour
{
  SpriteRenderer spriteRenderer;  //k
  CardModel model;  //k

  public AnimationCurve scaleCurve; //kk
  public float duration = 0.5f; //k

  private Vector3 originalScale;

  private void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();  //k
    model = GetComponent<CardModel>();  //k
    originalScale = transform.localScale; //dont scale cards size
  }

  public void FlipCard(Sprite startImage, Sprite endImage, int cardIndex) //k
  {
    StopAllCoroutines();//k
    StartCoroutine(Flip(startImage, endImage, cardIndex)); //k
  }

  IEnumerator Flip(Sprite startImage, Sprite endImage, int cardIndex)//k
  {
    spriteRenderer.sprite = startImage; //k
    float time = 0f;  //k

    while (time <= 1f)//k
    {
      float scaler = scaleCurve.Evaluate(time); //k

      //preserve orig size
      transform.localScale = new Vector3(originalScale.x * Mathf.Abs(scaler), originalScale.y, originalScale.z);

      if (time >= 0.5f)//k
        spriteRenderer.sprite = endImage;//k
        time += Time.deltaTime / duration;//k
        yield return null;  //k
      }

      transform.localScale = originalScale; //reset scale

      if(cardIndex == -1) //k
      {
        model.ToggleFace(false);//k
      }
      else
      {
        model.cardIndex = cardIndex;//k
        model.ToggleFace(true);//k
      }
  }
}



