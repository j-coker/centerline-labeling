using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterlineLabelingController : MonoBehaviour
{
	enum MAP_VIEW_MODE
	{
		COLOR_ID,
		BIOME_TYPE
	}

	List<Polyline> polylines = new List<Polyline>();

	[SerializeField] private MAP_VIEW_MODE mapViewmode;
	[SerializeField] private Renderer imageMesh;
	[SerializeField] private GameObject labelPrefab;
	[SerializeField] private float mapScale = 100f;
	[SerializeField] public int MapWidth = 8192;
	[SerializeField] public int MapHeight = 8192;

	[Header("Lines")]
	[SerializeField] private bool createLines = true;
	[SerializeField] private float widthMultiplier = 20f;
	[SerializeField] public Color LineColor = Color.white;
	[SerializeField] private int splineInterpolationPoints = 3;
	[SerializeField] private float cornerRadius = 0.1f;
	[SerializeField] private float simplifyTolerance = 0.1f;
	[SerializeField] private Texture2D lineTexture;
	[SerializeField] private float textureScale = 1f;
	[SerializeField] private float depthOffset = -1f;
	[SerializeField] private int layer = 1;
	[SerializeField] private int renderOrder = 4000;
	[SerializeField] private float viewOffset = 0f;
	[SerializeField] private float feather = 3f;
	[SerializeField] private bool transparent = false;

	[Header("Image Data")]
	[SerializeField] public Texture2D ProvinceIdTex;
	[SerializeField] private Texture2D terrainNormalTex;
	[SerializeField] private Texture2D lookupTex;
	[SerializeField] private RenderTexture distanceTransformTex;
	[SerializeField] private Texture2D colorMapIndirectionTex;
	[SerializeField] private Texture2D displayedColorMapTex;
	[SerializeField] private Texture2D biomeColorMapTex;

	[Header("GPU")]
	[SerializeField] private ComputeShader distanceFieldCS;
	[SerializeField] private ComputeShader createIndirectionMapCS;
	[SerializeField] private ComputeShader getTerritoryPixelsCS;
	[SerializeField] private ComputeShader getProvinceEdgePixelsCS;
	[SerializeField] private ComputeShader getBorderBitmaskCS;

	[Header("Provinces")]
	private List<Province> ProvincesInImage;

	[HideInInspector] public Dictionary<int, Biome> BiomeDict;
	[HideInInspector] public Dictionary<Color32, Province> ProvinceColorDict;
	[HideInInspector] public Dictionary<int, Province> ProvinceIntIDDict;
	[HideInInspector] public Dictionary<int, Border> BorderIDDict;
	[HideInInspector] public List<Border> BorderList;

	private void GenerateAllLabels()
	{
		StartCoroutine(GenerateAllLabelsCoroutine());
	}

	IEnumerator GenerateAllLabelsCoroutine()
	{
		float timer = Time.realtimeSinceStartup;

		for (int j = 1; j < ProvincesInImage.Count; j++)
		{
			Province operativeProvince = ProvincesInImage[j];

			List<Vector2Int> points = CenterlineLabeling.ComputeLSF(
				operativeProvince.ColorID,
				operativeProvince.PixelsX,
				operativeProvince.PixelsY,
				operativeProvince.MinXY,
				operativeProvince.MaxXY,
				ProvinceIdTex,
				out float[] midpointDistances,
				out bool isIdeal,
				20,
				450f);

			if (points.Count == 3)
			{
				GameObject label = GameObject.Instantiate(labelPrefab);
				TMPro.TMP_Text text = label.GetComponent<TMPro.TMP_Text>();
				TextProOnASpline spline = label.GetComponent<TextProOnASpline>();

				text.text = "Texas";

				spline.SetPoints(points, isIdeal, midpointDistances, operativeProvince.ColorID, ProvinceIdTex, 100f, 8192, 8192);
			}
		}

		timer = Time.realtimeSinceStartup - timer;
		Debug.Log("Label generation operation complete. Time: " + ti mer);
		yield return null;
	}
}
