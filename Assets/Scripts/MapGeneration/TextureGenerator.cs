using UnityEngine;
using System.Collections;

public static class TextureGenerator {

	public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height) {
		Texture2D texture = new Texture2D (width, height);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels (colourMap);
		texture.Apply ();
		return texture;
	}

    // wow wtf happening? 
    public static Texture2D TextureFromHeightMap(TerrainPoint[,] heightMap) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                // Read from the .finalHeight of the struct!
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y].finalHeight);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

    // Version 2: For raw float grids (Used by DrawMode.FalloffMap)
    public static Texture2D TextureFromHeightMap(float[,] heightMap) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

}
