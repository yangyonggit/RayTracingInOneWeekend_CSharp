using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Chapter8
{
    public struct Hit_record
    {
        public float t;
        public Vector3 hitpoint;
        public Vector3 normal;
        public IMaterial mat;
    };

    public interface IMaterial
    {
        bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered);
    };



    public interface Hitable
    {
        bool Hit(Ray r, ref float t_min, ref float t_max, out Hit_record record);
    };

    public class HitList : Hitable
    {
        private List<Hitable> list = new List<Hitable>();

        public HitList()
        {
        }

        public int GetCount()
        {
            return list.Count;
        }

        public void Add(Hitable item)
        {
            list.Add(item);
        }

        public bool Hit(Ray r, ref float t_min, ref float t_max, out Hit_record record)
        {
            Hit_record temp_rec = new Hit_record();
            record = temp_rec;
            bool hit_anything = false;
            float closest_so_far = t_max;

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i].Hit(r, ref t_min, ref closest_so_far, out temp_rec))
                {
                    hit_anything = true;
                    closest_so_far = temp_rec.t;
                    record = temp_rec;
                }
            }

            return hit_anything;
        }
    };


    public class Lambertian : IMaterial
    {
        public Vector3 albedo;
        public float reflect;

        public Lambertian(Vector3 a,float r)
        {
            albedo = a;
            reflect = r;
        }

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
        {
            Vector3 target = rec.normal.normalized  +
                new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

            scattered.origin = rec.hitpoint;
            scattered.direction = target;
            attenuation = albedo * reflect;
            return true;
        }
    }


    public class MetalNoFuzz : IMaterial
    {
        public Vector3 albedo;

        public MetalNoFuzz(Vector3 a)
        {
            albedo = a;
        }

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
        {
            Vector3 reflected = Vector3.Reflect(r.direction.normalized, rec.normal.normalized);

            scattered.origin = rec.hitpoint;
            scattered.direction = reflected;
            attenuation = albedo;
            return Vector3.Dot(scattered.direction, rec.normal) > 0;
        }
    }

    public class Metal : IMaterial
    {
        public Vector3 albedo;
        public float fuzz;

        public Metal(Vector3 a,float f)
        {
            albedo = a;
            fuzz = f;
        }

        private Vector3 Random_in_unit_sphere()
        {
            return new Vector3(Random.Range(-1f, 1f), Random.Range(-1, 1f), Random.Range(-1f, 1f)).normalized;
        }

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
        {
            Vector3 reflected = Vector3.Reflect(r.direction.normalized, rec.normal.normalized);
            reflected = reflected + fuzz * Random_in_unit_sphere();

            scattered.origin = rec.hitpoint;
            scattered.direction = reflected;
            attenuation = albedo;
            return Vector3.Dot(scattered.direction, rec.normal) > 0;
        }
    }

    ////////////////////////////////////////
    public class Sphere : Hitable
    {
        public Vector3 center;

        public float radius;

        public IMaterial mat;

        public Sphere()
        {
            center = Vector3.zero;
            radius = 1;
        }

        public Sphere(Vector3 c, float r)
        {
            center = c;
            radius = r;
        }

        public Sphere(Vector3 c, float r, IMaterial m)
        {
            center = c;
            radius = r;
            mat = m;
        }

        public bool Hit(Ray ray, ref float t_min, ref float t_max, out Hit_record record)
        {
            record = new Hit_record();
            Vector3 oc = ray.origin - center;
            float a = Vector3.Dot(ray.direction, ray.direction);
            float b = Vector3.Dot(oc, ray.direction);
            float c = Vector3.Dot(oc, oc) - radius * radius;
            float d = b * b - a * c;
            if (d > 0)
            {
                float temp = (-b - Mathf.Sqrt(d)) / a;
                if (temp < t_max && temp > t_min)
                {
                    record.t = temp;
                    record.hitpoint = ray.GetPoint(temp);
                    record.normal = (record.hitpoint - center) / radius;
                    record.mat = mat;
                    return true;
                }
                temp = (-b + Mathf.Sqrt(d)) / a;
                if (temp < t_max && temp > t_min)
                {
                    record.t = temp;
                    record.hitpoint = ray.GetPoint(temp);
                    record.normal = (record.hitpoint - center) / radius;
                    record.mat = mat;
                    return true;
                }
            }
            return false;
        }
    };

    public class Chapter8
    {
        private static Vector3 topColor = Vector3.one;
        private static Vector3 bottomColor = new Vector3(0.5f, 0.8f, 1.0f);
        private static int MaxDepth = 50;

        public static Vector3 RayCast(Ray ray, Hitable world, int depth)
        {
            Hit_record rec;
            float min = 0;
            float max = float.MaxValue;
            if (world.Hit(ray, ref min, ref max, out rec))
            {
                Ray scattered = new Ray();
                Vector3 attenuation = Vector3.one;

                if (depth < MaxDepth && rec.mat.Scatter(ref ray, ref rec, ref attenuation, ref scattered))
                {
                    var color = RayCast(scattered, world, depth + 1);
                    attenuation.x *= color.x;
                    attenuation.y *= color.y;
                    attenuation.z *= color.z;
                    return attenuation;
                }
                else
                {
                    return Vector3.zero;
                }
            }
            else
            {
                Vector3 unit_direction = ray.direction.normalized;
                float t = 0.5f * (unit_direction.y + 1.0f);
                return Vector3.Lerp(topColor, bottomColor, t);
            }
        }

        [MenuItem("Raytracing/Chapter8/1")]
        public static void MainMetalNoFuzz()
        {
            int nx = 1280;
            int ny = 640;
            int ns = 64;

            RayCamera camera = new RayCamera();

            HitList list = new HitList();
            list.Add(new Sphere(new Vector3(0, 0, -1), 0.5f, new Lambertian(new Vector3(0.8f, 0.3f, 0.3f),0.382f)));
            list.Add(new Sphere(new Vector3(0, -100.5f, -1), 100, new Lambertian(new Vector3(0.8f, 0.8f, 0.0f),0.618f)));
            list.Add(new Sphere(new Vector3(1, 0, -1), 0.5f, new MetalNoFuzz(new Vector3(0.8f, 0.6f, 0.2f))));
            list.Add(new Sphere(new Vector3(-1, 0, -1), 0.5f, new MetalNoFuzz(new Vector3(0.8f, 0.8f, 0.8f))));
            Texture2D tex = ImageHelper.CreateImg(nx, ny);

            for (int j = ny - 1; j >= 0; --j)
            {
                for (int i = 0; i < nx; ++i)
                {
                    Vector3 color = Vector3.zero;
                    for (int k = 0; k < ns; ++k)
                    {
                        float u = (float)(i + Random.Range(-1f, 1f)) / (float)(nx);
                        float v = (float)(j + Random.Range(-1f, 1f)) / (float)(ny);

                        Ray r = camera.GetRay(u, v);
                        color += RayCast(r, list, 0);
                    }
                    color = color / (float)(ns);
                    color.x = Mathf.Sqrt(color.x);
                    color.y = Mathf.Sqrt(color.y);
                    color.z = Mathf.Sqrt(color.z);
                    ImageHelper.SetPixel(tex, i, j, color);
                }
            }

            ImageHelper.SaveImg(tex, "Img/chapter8_1.png");
            Debug.Log("Chapter 8 is done");
        }


        [MenuItem("Raytracing/Chapter8/2")]
        public static void Main()
        {
            int nx = 1280;
            int ny = 640;
            int ns = 64;

            RayCamera camera = new RayCamera();

            HitList list = new HitList();
            list.Add(new Sphere(new Vector3(0, 0, -1), 0.5f, new Lambertian(new Vector3(0.8f, 0.3f, 0.3f), 0.382f)));
            list.Add(new Sphere(new Vector3(0, -100.5f, -1), 100, new Lambertian(new Vector3(0.8f, 0.8f, 0.0f), 0.618f)));
            list.Add(new Sphere(new Vector3(1, 0, -1), 0.5f, new Metal(new Vector3(0.8f, 0.6f, 0.2f),0.3f)));
            list.Add(new Sphere(new Vector3(-1, 0, -1), 0.5f, new Metal(new Vector3(0.8f, 0.8f, 0.8f),1.0f)));
            Texture2D tex = ImageHelper.CreateImg(nx, ny);

            for (int j = ny - 1; j >= 0; --j)
            {
                for (int i = 0; i < nx; ++i)
                {
                    Vector3 color = Vector3.zero;
                    for (int k = 0; k < ns; ++k)
                    {
                        float u = (float)(i + Random.Range(-1f, 1f)) / (float)(nx);
                        float v = (float)(j + Random.Range(-1f, 1f)) / (float)(ny);

                        Ray r = camera.GetRay(u, v);
                        color += RayCast(r, list, 0);
                    }
                    color = color / (float)(ns);
                    color.x = Mathf.Sqrt(color.x);
                    color.y = Mathf.Sqrt(color.y);
                    color.z = Mathf.Sqrt(color.z);
                    ImageHelper.SetPixel(tex, i, j, color);
                }
            }

            ImageHelper.SaveImg(tex, "Img/chapter8fuzz2.png");
            Debug.Log("Chapter 8 fuzz is done");
        }
    }
}