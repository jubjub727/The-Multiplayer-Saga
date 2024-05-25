using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tmpsclient
{
    public class PrefabList
    {
        private Dictionary<string, string> Characters = new Dictionary<string, string>();

        private string[] Prefabs;

        public string GetCharacterNameFromPrefab(string prefab)
        {
            string[] parts = prefab.Split('/');
            string name = parts[parts.Length-1];
            return name.Split('.')[0];
        }

        public string GetPrefabFromCharacterName(string characterName)
        {
            return Characters[characterName];
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
