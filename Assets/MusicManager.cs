using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager singleton;
    AudioSource mySource;

    private void Awake()
    {
        if (singleton != null)
        {
            Destroy(gameObject);
            return;
        }

        singleton = this;
        DontDestroyOnLoad(gameObject);
        mySource = GetComponent<AudioSource>();
    }

    public void ToggleMute()
    {
        mySource.mute = !mySource.mute;
    }
}
