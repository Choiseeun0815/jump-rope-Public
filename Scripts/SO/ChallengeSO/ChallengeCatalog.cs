using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Challenge/Catalog")]
public class ChallengeCatalog : ScriptableObject
{
    public List<ChallengeItemDefinition> items;

    public ChallengeItemDefinition GetById(string id) =>
        items.FirstOrDefault(i => i.id == id);
}