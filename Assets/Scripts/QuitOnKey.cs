using UnityEngine;
using System.Collections;

public class QuitOnKey : MonoBehaviour
{
  public AudioSource audioSource;
  public AudioClip quitSound;
  public KeyCode quitKey = KeyCode.Mouse1; //can be overwritten in editor
  //private bool isQuitting = false;

  void Update()
  {
    //if (Input.GetKeyDown(quitKey) && !isQuitting)
    if (Input.GetKeyDown(quitKey))
    {
      StartCoroutine(QuitRoutine());
    }
  }
  IEnumerator QuitRoutine()
  {
    //isQuitting = true;
    GetComponent<AudioSource>().Play();
    yield return new WaitForSeconds(quitSound.length +0.5f); // you can extend (quitSound.length +1)
    Application.Quit();
    
    #if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
    #endif
  }
}
