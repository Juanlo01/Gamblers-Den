using System;
using FMODUnity;
using UnityEngine;

namespace SimpleAudioSystem
{
    [Serializable]
    public class AudioEntry
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private EventReference eventReference;

        [SerializeField]
        private AudioCategory category;

        public string Id => id;
        public EventReference EventReference => eventReference;
        public AudioCategory Category => category;
    }
}
