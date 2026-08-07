using UnityEngine;

namespace ClubPenguin.WorldEditor.Optimization
{
	[DisallowMultipleComponent]
	public class SceneOptimizationSettings : MonoBehaviour
	{
		[Header("Texture Atlas")]
		public int MinTextureSize = 2048;

		public int MaxTextureSize = 2048;

		public int MaxAtlasDimension = 2048;

		[Header("Texture Atlas Preview")]
		public Texture2D[] TextureAtlasPreviewButtons;

		[Header("Results")]
		public TextureData[] Textures;

		public Texture2D WorldObjectTextureAtlas;

		public Material WorldObjectAtlasMaterial;
	}
}
