using UnityEngine;
using UIModule;


public class GameMain : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // TitleScreen 로드
        UIManager.Instance.ShowScreen<TitleScreen>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
