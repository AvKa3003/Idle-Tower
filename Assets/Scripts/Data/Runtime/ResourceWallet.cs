using System;
using System.Collections.Generic;
using IdleTower.Data.Definitions;

namespace IdleTower.Data.Runtime
{
    /// <summary>
    /// Runtime-ключ — ссылка на ResourceDefinition.
    /// В сейве — <see cref="ResourceId.Value"/>; при загрузке резолв через каталог AllResources.
    /// </summary>
    public class ResourceWallet
    {
        private readonly Dictionary<ResourceDefinition, int> _amounts = new();

        public IReadOnlyDictionary<ResourceDefinition, int> Amounts => _amounts;

        public int GetAmount(ResourceDefinition resource)
        {
            EnsureValidResource(resource);
            return _amounts.TryGetValue(resource, out var amount) ? amount : 0;
        }

        public void SetAmount(ResourceDefinition resource, int amount)
        {
            EnsureValidResource(resource);
            _amounts[resource] = amount < 0 ? 0 : amount;
        }

        public void Add(ResourceDefinition resource, int delta)
        {
            if (delta == 0)
                return;

            var current = GetAmount(resource);
            SetAmount(resource, current + delta);
        }

        public void Clear()
        {
            _amounts.Clear();
        }

        /// <summary>Снимок для сейва: стабильный string Id + количество.</summary>
        public List<ResourceAmountSave> CaptureForSave()
        {
            var list = new List<ResourceAmountSave>(_amounts.Count);
            foreach (var pair in _amounts)
            {
                list.Add(new ResourceAmountSave
                {
                    ResourceId = pair.Key.Id.Value,
                    Amount = pair.Value
                });
            }

            return list;
        }

        /// <summary>
        /// Загрузка сейва: Id → asset из каталога. Неизвестный Id — ошибка (не молчаливый skip).
        /// </summary>
        public void ApplyFromSave(
            IReadOnlyList<ResourceAmountSave> entries,
            IReadOnlyDictionary<ResourceId, ResourceDefinition> catalogById)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            if (catalogById == null)
                throw new ArgumentNullException(nameof(catalogById));

            Clear();

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var resourceId = ResourceId.FromSerialized(entry.ResourceId);
                if (resourceId.IsEmpty)
                {
                    throw new InvalidOperationException(
                        $"[ResourceWallet.ApplyFromSave] entries[{i}]: пустой ResourceId.");
                }

                if (!catalogById.TryGetValue(resourceId, out var resource) || resource == null)
                {
                    throw new InvalidOperationException(
                        $"[ResourceWallet.ApplyFromSave] неизвестный ResourceId '{resourceId.Value}'.");
                }

                SetAmount(resource, entry.Amount);
            }
        }

        private static void EnsureValidResource(ResourceDefinition resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            if (resource.Id.IsEmpty)
            {
                throw new InvalidOperationException(
                    $"[ResourceWallet] У ресурса '{resource.name}' пустой Id.");
            }
        }
    }

    /// <summary>DTO сейва: JsonUtility сериализует string, не ResourceId.</summary>
    [Serializable]
    public struct ResourceAmountSave
    {
        public string ResourceId;
        public int Amount;
    }
}
