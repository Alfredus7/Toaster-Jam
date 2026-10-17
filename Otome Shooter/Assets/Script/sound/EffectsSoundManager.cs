using UnityEngine;
using System.Collections.Generic;

public class EffectsSoundManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    public List<Sound> sounds = new List<Sound>();
    private AudioSource audioSource;
    private Dictionary<string, AudioClip> soundDictionary = new Dictionary<string, AudioClip>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Llenar el diccionario
        foreach (Sound sound in sounds)
        {
            if (!soundDictionary.ContainsKey(sound.name))
            {
                soundDictionary.Add(sound.name, sound.clip);
            }
            else
            {
                Debug.LogWarning("Sound name duplicate: " + sound.name);
            }
        }
    }

    public void PlayEffect(string soundName)
    {
        if (soundDictionary.ContainsKey(soundName))
        {
            audioSource.PlayOneShot(soundDictionary[soundName]);
        }
        else
        {
            Debug.LogWarning("Sound not found: " + soundName);
        }
    }
}