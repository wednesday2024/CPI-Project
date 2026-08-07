using ClubPenguin.Net.Client.Mappers;
using ClubPenguin.Net.Domain.Decoration;
using ClubPenguin.Net.Domain.Scene;
using ClubPenguin.Net.Offline;
using hg.ApiWebKit.core.attributes;
using hg.ApiWebKit.mappers;
using System.Collections.Generic;

namespace ClubPenguin.Net.Client
{
	[HttpBasicAuthorization("cp-api-username", "cp-api-password")]
	[HttpGET]
	[HttpPath("cp-api-base-uri", "/igloo/v1/decorations")]
	[HttpAccept("application/json")]
	public class GetDecorationsOperation : CPAPIHttpOperation
	{
		[HttpResponseJsonBody]
		public DecorationInventory DecorationInventory;

		protected override void PerformOfflineAction(OfflineDatabase offlineDatabase, IOfflineDefinitionLoader offlineDefinitions)
		{
			DecorationInventory = new DecorationInventory();
			DecorationInventory.items = new List<DecorationInventoryItem>();
			DecorationInventoryEntity decorationInventoryEntity = offlineDatabase.Read<DecorationInventoryEntity>();
			if (decorationInventoryEntity.inventory != null)
			{
				foreach (KeyValuePair<string, int> item in decorationInventoryEntity.inventory)
				{
					DecorationInventory.items.Add(new DecorationInventoryItem
					{
						decorationId = DecorationId.FromString(item.Key),
						count = item.Value
					});
				}
			}
			if (DecorationInventory.items.Count == 0)
			{
				RebuildInventoryFromLayouts(offlineDatabase);
			}
		}

		private void RebuildInventoryFromLayouts(OfflineDatabase offlineDatabase)
		{
			SceneLayoutEntity sceneLayoutEntity = offlineDatabase.Read<SceneLayoutEntity>();
			if (sceneLayoutEntity.Layouts == null)
			{
				return;
			}
			Dictionary<string, int> rebuiltInventory = new Dictionary<string, int>();
			foreach (SavedSceneLayout layout in sceneLayoutEntity.Layouts)
			{
				if (layout.decorationsLayout == null)
				{
					continue;
				}
				foreach (DecorationLayout decoration in layout.decorationsLayout)
				{
					DecorationId decorationId = new DecorationId((int)decoration.definitionId, decoration.type);
					string key = decorationId.ToString();
					if (rebuiltInventory.ContainsKey(key))
					{
						rebuiltInventory[key]++;
					}
					else
					{
						rebuiltInventory[key] = 1;
					}
				}
			}
			foreach (KeyValuePair<string, int> item in rebuiltInventory)
			{
				DecorationInventory.items.Add(new DecorationInventoryItem
				{
					decorationId = DecorationId.FromString(item.Key),
					count = item.Value
				});
			}
			if (rebuiltInventory.Count > 0)
			{
				DecorationInventoryEntity value = offlineDatabase.Read<DecorationInventoryEntity>();
				value.inventory = rebuiltInventory;
				offlineDatabase.Write(value);
			}
		}

		protected override void SetOfflineData(OfflineDatabase offlineDatabase, IOfflineDefinitionLoader offlineDefinitions)
		{
			DecorationInventoryEntity value = offlineDatabase.Read<DecorationInventoryEntity>();
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (DecorationInventoryItem item in DecorationInventory.items)
			{
				dictionary.Add(item.decorationId.ToString(), item.count);
			}
			value.inventory = dictionary;
			offlineDatabase.Write(value);
		}
	}
}
