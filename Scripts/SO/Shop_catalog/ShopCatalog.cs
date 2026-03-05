using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Catalog")]
public class ShopCatalog : ScriptableObject
{
    public List<ShopItemDefinition> items;

    public List<ShopItemDefinition> GetByCategory(ShopCategory category) =>
        items.Where(i => i.category == category).ToList();

    public ShopItemDefinition GetById(string id) =>
        items.FirstOrDefault(i => i.id == id);
}