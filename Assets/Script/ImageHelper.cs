using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ImageHelper
{
    public static Texture2D CreateImg(int width ,int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        return tex;        
    }

    public static void SetPixel(Texture2D tex,int x,int y,float r,float g,float b)
    {
        tex.SetPixel(x, y, new Color(r, g, b));
    }

    public static void SetPixel(Texture2D tex,int x,int y,Vector3 color)
    {
        tex.SetPixel(x, y, new Color(color.x, color.y, color.z));
    }

    public static void SaveImg(Texture2D tex, string path)
    {
        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Path.Combine(Application.dataPath, path), bytes);
    }
}