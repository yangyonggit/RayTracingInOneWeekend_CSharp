using UnityEngine;
using UnityEditor;

public class Chapter4
{
    private static Vector3 topColor = Vector3.one;
    private static Vector3 bottomColor = new Vector3(0.5f, 0.7f, 1.0f);
    private static Vector3 ballColor = new Vector3(1, 0, 0);

    private static Vector3 center = new Vector3(0, 0, -1);
    private static float radius = 0.5f;

   public static bool Hit_sphere(Vector3 center,float radius,Ray ray)
    {
        Vector3 oc = ray.origin - center;
        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2.0f * Vector3.Dot(oc, ray.direction);
        float c = Vector3.Dot(oc, oc) - radius * radius;
        float d = b * b - 4 * a * c;
        return d > 0;
    }

    public static Vector3 RayCast(Ray ray)
    {
        if (Hit_sphere(center, radius, ray))
        {
            return ballColor;
        }
        Vector3 unit_direction = ray.direction.normalized;
        float t = 0.5f * (unit_direction.y + 1.0f);
        return Vector3.Lerp(topColor, bottomColor, t);
    }

    [MenuItem("Raytracing/Chapter4")]
    public static void Main()
    {
        int nx = 1280;
        int ny = 640;

        Vector3 lower_left_corner = new Vector3(-2.0f, -1.0f, -1.0f);
        Vector3 horizontal = new Vector3(4.0f, 0.0f, 0.0f);
        Vector3 vertical = new Vector3(0.0f, 2.0f, 0.0f);
        Vector3 origin = Vector3.zero;

        Texture2D tex = ImageHelper.CreateImg(nx, ny);
        for (int j = ny - 1; j >= 0; --j)
        {
            for (int i = 0; i < nx; ++i)
            {
                float u = (float)(i) / (float)(nx);
                float v = (float)(j) / (float)(ny);

                Ray r = new Ray(origin, lower_left_corner + u * horizontal + v * vertical);
                Vector3 color = RayCast(r);

                ImageHelper.SetPixel(tex, i, j, color);
            }
        }

        ImageHelper.SaveImg(tex, "Img/Chapter4.png");
    }
}