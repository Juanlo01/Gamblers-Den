using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleAudioSystem
{
    [CreateAssetMenu(
        fileName = "AudioDatabase",
        menuName = "Simple Audio System/Audio Database")]
    public class AudioDatabase : ScriptableObject
    {
        [SerializeField]
        private List<AudioEntry> entries = new List<AudioEntry>();

        [NonSerialized]
        private Dictionary<string, AudioEntry> lookup;

        [NonSerialized]
        private bool lookupBuilt;

        public bool TryGetEntry(string id, out AudioEntry entry)
        {
            if (!lookupBuilt)
            {
                BuildLookup();
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                entry = null;
                return false;
            }

            return lookup.TryGetValue(id, out entry);
        }

        private void OnEnable()
        {
            BuildLookup();
        }

        private void OnValidate()
        {
            lookupBuilt = false;
        }

        private void BuildLookup()
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, AudioEntry>(StringComparer.Ordinal);
            }
            else
            {
                lookup.Clear();
            }

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    AudioEntry entry = entries[i];
                    if (entry == null ||
                        string.IsNullOrWhiteSpace(entry.Id) ||
                        entry.EventReference.IsNull)
                    {
                        continue;
                    }

                    if (lookup.ContainsKey(entry.Id))
                    {
                        Debug.LogWarning(
                            $"AudioDatabase '{name}' contains duplicate ID " +
                            $"'{entry.Id}'. The first valid entry will be used.",
                            this);
                        continue;
                    }

                    lookup.Add(entry.Id, entry);
                }
            }

            lookupBuilt = true;
        }
    }
}
