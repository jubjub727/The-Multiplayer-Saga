using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tmpsclient
{
    public class PrefabList
    {
        private List<string> CachedNames = new List<string>();
        private List<string> CachedPrefabs = new List<string>();

        private Dictionary<string, string> Characters = new Dictionary<string, string>();

        private string[] Prefabs;

        public string GetCharacterNameFromPrefab(string prefab)
        {
            string[] parts = prefab.Split('/');
            string name = parts[parts.Length-1];
            return name.Split('.')[0];
        }

        private string? GetCachedPrefabName(string characterName)
        {
            for (int i = 0; i < CachedNames.Count; i++)
            {
                if (CachedNames[i] == characterName)
                {
                    return CachedPrefabs[i];
                }
            }

            return null;
        }

        public string GetPrefabFromCharacterName(string characterName)
        {
            string? cachedName = GetCachedPrefabName(characterName);
            if (cachedName == null)
            {
                if (Characters.ContainsKey(characterName))
                {
                    string prefabName = Characters[characterName];

                    CachedNames.Add(characterName);
                    CachedPrefabs.Add(prefabName);

                    if (CachedNames.Count > 32)
                    {
                        CachedNames.RemoveAt(0);
                        CachedPrefabs.RemoveAt(0);
                    }

                    return prefabName;
                }
                else
                {
                    string prefabName = "Chars/Minifig/Stormtrooper/Stormtrooper.prefab_baked";

                    CachedNames.Add(characterName);
                    CachedPrefabs.Add(prefabName);

                    if (CachedNames.Count > 10)
                    {
                        CachedNames.RemoveAt(0);
                        CachedPrefabs.RemoveAt(0);
                    }

                    return prefabName;
                }
            }
            else
            {
                return cachedName;
            }
        }

        private void AddPrefab(string prefab)
        {
            Characters.Add(GetCharacterNameFromPrefab(prefab), prefab);
        }

        private void LoadCharacterNames()
        {
            foreach (string prefab in Prefabs)
            {
                AddPrefab(prefab);
            }
        }

        public PrefabList(string prefabListPath)
        {
            Prefabs = File.ReadAllLines(prefabListPath);
            LoadCharacterNames();
        }
    }
}
