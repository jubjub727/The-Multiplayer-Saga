using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tmpsclient
{
    public class PrefabList
    {
        public Dictionary<string, string> Characters;

        public string GetCharacterNameFromPrefab(string prefab)
        {
            string[] parts = prefab.Split('/');
            string name = parts[parts.Length-1];
            return name.Split('.')[0];
        }

        private void AddPrefab(string prefab)
        {
            Characters.Add(GetCharacterNameFromPrefab(prefab), prefab);
        }

        public PrefabList(string prefabListPath)
        {
            string[] prefabs = File.ReadAllLines(prefabListPath);

            foreach (string prefab in prefabs)
            {
                AddPrefab(prefab);
            }   
        }
    }
}
