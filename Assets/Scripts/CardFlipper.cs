using System.Collections;
//using UnityEditor.Experimental.GraphView; //had to remove it, causes error building
using UnityEngine;

public class CardFlipper : MonoBehaviour
{
  SpriteRenderer spriteRenderer;
  CardModel model;

  public AnimationCurve scaleCurve;
  public float duration = 0.5f;

  private Vector3 originalScale;

  private void Awake()
  {
    spriteRenderer = GetComponent<SpriteRenderer>();
    model = GetComponent<CardModel>();
    originalScale = transform.localScale; //dont scale cards size
  }

  public void FlipCard(Sprite startImage, Sprite endImage, int cardIndex)
  {
    StopAllCoroutines();//k
    StartCoroutine(Flip(startImage, endImage, cardIndex));
  }

  IEnumerator Flip(Sprite startImage, Sprite endImage, int cardIndex)
  {
    spriteRenderer.sprite = startImage;
    float time = 0f;

    while (time <= 1f)
    {
      float scaler = scaleCurve.Evaluate(time);

      //preserve orig size
      transform.localScale = new Vector3(originalScale.x * Mathf.Abs(scaler), originalScale.y, originalScale.z);

      if (time >= 0.5f)
        spriteRenderer.sprite = endImage;
        time += Time.deltaTime / duration;
        yield return null;
      }


      transform.localScale = originalScale; //reset scale

      if(cardIndex == -1)
      {
        model.ToggleFace(false);
      }
      else
      {
        model.cardIndex = cardIndex;
        model.ToggleFace(true);
      }
  }
}



