using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class RayCamera
{
    private Vector3 origin;
    private Vector3 lower_left_corner;
    private Vector3 horizontal;
    private Vector3 vertical;


    public RayCamera()
    {
        origin = Vector3.zero;
        lower_left_corner = new Vector3(-2.0f, -1.0f, -1.0f);
        horizontal = new Vector3(4, 0, 0);
        vertical = new Vector3(0, 2, 0);
    }

    public RayCamera(Vector3 ori,Vector3 corner,Vector3 h,Vector3 v)
    {
        origin = ori;
        lower_left_corner = corner;
        horizontal = h;
        vertical = v;
    }

    public RayCamera(float fov, float aspect)
    {
        float theta = Mathf.Deg2Rad * fov;
        float half_height = Mathf.Tan(theta * 0.5f);
        float half_width = aspect * half_height;
        lower_left_corner = new Vector3(-half_width, -half_height, -1.0f);
        horizontal = new Vector3(2 * half_width, 0, 0);
        vertical = new Vector3(0, 2 * half_height, 0);
        origin = Vector3.zero;
    }

    public RayCamera(Vector3 lookfrom,Vector3 lookat,Vector3 vup, float fov, float aspect)
    {
        Vector3 u, v, w;
        float theta = Mathf.Deg2Rad * fov;
        float half_height = Mathf.Tan(theta * 0.5f);
        float half_width = aspect * half_height;
        origin = lookfrom;
        w = (lookfrom - lookat).normalized;
        u = Vector3.Cross(vup, w).normalized;
        v = Vector3.Cross(w, u);

        lower_left_corner = new Vector3(-half_width, -half_height, -1.0f);
        lower_left_corner = origin - half_width * u - half_height * v - w;
        horizontal = 2 * half_width * u;
        vertical = 2 * half_height * v;
    }


    public Ray GetRay(float u,float v)
    {
        return new Ray(origin, lower_left_corner + u * horizontal + v * vertical - origin);
    }

}


public class MotionBlurRayCamera
{
    private Vector3 origin;
    private Vector3 lower_left_corner;
    private Vector3 horizontal;
    private Vector3 vertical;
    private Vector3 u, v, w;
    private float lens_radius;
    private float focus;
    System.Random random = new System.Random();

    private Vector2 Random_in_unit_disk()
    {
        Vector2 v = new Vector2((float)(random.NextDouble() * 2 - 1.0f), 
                            (float)(random.NextDouble() * 2 - 1.0f));
        return v;
    }

    private Vector2 Random_in_unit_disk(System.Random seed)
    {
        Vector2 v = new Vector2((float)(seed.NextDouble() * 2 - 1.0f),
                            (float)(seed.NextDouble() * 2 - 1.0f));
        return v;
    }

    public MotionBlurRayCamera(Vector3 lookfrom, Vector3 lookat, Vector3 vup, float fov, float aspect,float aperture,float focus_dist)
    {
        lens_radius = aperture / 2;
        float theta = Mathf.Deg2Rad * fov;
        float half_height = Mathf.Tan(theta * 0.5f);
        float half_width = aspect * half_height;
        origin = lookfrom;
        w = (lookfrom - lookat).normalized;
        u = Vector3.Cross(vup, w).normalized;
        v = Vector3.Cross(w, u);
        focus = focus_dist;

        lower_left_corner = origin - half_width * focus_dist * u - half_height* focus_dist * v - focus_dist * w;
        horizontal = 2 * half_width * focus_dist * u; 
        vertical = 2 * half_height * focus_dist * v;
    }


    public Ray GetRay(float s, float t)
    {
        Vector2 rd = lens_radius * Random_in_unit_disk();
        Vector3 offset = u * rd.x + v * rd.y;        
        return new Ray(origin + offset, lower_left_corner + s * horizontal + t * vertical - origin - offset);
    }

    public Ray GetRay(float s, float t,System.Random seed)
    {
        Vector2 rd = lens_radius * Random_in_unit_disk(seed);
        Vector3 offset = u * rd.x + v * rd.y;
        return new Ray(origin + offset, lower_left_corner + s * horizontal + t * vertical - origin - offset);
    }
}