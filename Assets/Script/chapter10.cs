using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Chapter10
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

        public Lambertian(Vector3 a)
        {
            albedo = a;
        }

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
        {
            Vector3 target = rec.normal.normalized * 0.5f +
                new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

            scattered.origin = rec.hitpoint;
            scattered.direction = target;
            attenuation = albedo;
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

    public class Dielectric : IMaterial
    {
        public float ref_idx;

        public Dielectric(float r)
        {
            ref_idx = r;
        }

        private bool Refract(Vector3 v, Vector3 n, float ni_over_nt,out Vector3 refracted)
        {

            v.Normalize();
            n.Normalize();
            float dt = Vector3.Dot(v.normalized, n.normalized);
            float discriminant = 1.0f - ni_over_nt * ni_over_nt * (1.0f - dt * dt);
            if (discriminant > 0)
            {
                refracted = ni_over_nt * (v.normalized - n * dt) - n * Mathf.Sqrt(discriminant);
                return true;
            }
            refracted = Vector3.one;
            return false;
        }

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
        {
            Vector3 outward_normal = Vector3.zero;
            Vector3 reflected = Vector3.Reflect(r.direction.normalized, rec.normal.normalized);
            float ni_over_nt = 0f;
            attenuation.x = 1.0f;
            attenuation.y = 1.0f;
            attenuation.z = 1.0f;
            Vector3 refracted;
            if (Vector3.Dot(r.direction,rec.normal) > 0)
            {
                outward_normal = -rec.normal;
                ni_over_nt = ref_idx;
            }
            else
            {
                outward_normal = rec.normal;
                ni_over_nt = 1.0f / ref_idx;
            }

            if (Refract(r.direction,outward_normal,ni_over_nt,out refracted))
            {
                scattered.origin = rec.hitpoint;
                scattered.direction = refracted;
                return true;
            }
            else
            {
                scattered.origin = rec.hitpoint;
                scattered.direction = reflected;
                return false;
            }
        }
    }

    public class DielectricShlick : IMaterial
    {
        public float ref_idx;

        public DielectricShlick(float r)
        {
            ref_idx = r;
        }

        private bool Refract(Vector3 v, Vector3 n, float ni_over_nt, out Vector3 refracted)
        {

            v.Normalize();
            n.Normalize();
            float dt = Vector3.Dot(v.normalized, n.normalized);
            float discriminant = 1.0f - ni_over_nt * ni_over_nt * (1.0f - dt * dt);
            if (discriminant > 0)
            {
                refracted = ni_over_nt * (v.normalized - n * dt) - n * Mathf.Sqrt(discriminant);
                return true;
            }
            refracted = Vector3.one;
            return false;
        }

        private float Schlick(float cosine,float ref_idx)
        {
            float r0 = (1.0f - ref_idx) / (1 + ref_idx);
            r0 *= r0;
            return r0 + (1 - r0) * Mathf.Pow((1 - cosine), 5);
        }

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered)
        {
            Vector3 outward_normal = Vector3.zero;
            Vector3 reflected = Vector3.Reflect(r.direction.normalized, rec.normal.normalized);
            float ni_over_nt = 0f;
            attenuation.x = 1.0f;
            attenuation.y = 1.0f;
            attenuation.z = 1.0f;
            Vector3 refracted;
            float reflect_prob;
            float cosine;
            if (Vector3.Dot(r.direction, rec.normal) > 0)
            {
                outward_normal = -rec.normal;
                ni_over_nt = ref_idx;
                cosine = ref_idx * Vector3.Dot(r.direction, rec.normal) / r.direction.magnitude;
            }
            else
            {
                outward_normal = rec.normal;
                ni_over_nt = 1.0f / ref_idx;
                cosine = -Vector3.Dot(r.direction.normalized, rec.normal) / r.direction.magnitude;
            }

            var bRefracted = Refract(r.direction, outward_normal, ni_over_nt, out refracted);
            if (bRefracted)
            {
                reflect_prob = Schlick(cosine, ref_idx);
            }
            else
            {
                scattered.origin = rec.hitpoint;
                scattered.direction = reflected;
                reflect_prob = 1.0f;
            }
            if (Random.Range(0, 1) < reflect_prob)
            {
                scattered.origin = rec.hitpoint;
                scattered.direction = reflected;
            }
            else
            {
                scattered.origin = rec.hitpoint;
                scattered.direction = refracted;
            }
            return true;
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

    public class Chapter10
    {
        private static Vector3 topColor = Vector3.one;
        private static Vector3 bottomColor = new Vector3(0.5f, 0.9f, 1.0f);

        public static Vector3 RayCast(Ray ray, Hitable world, int depth)
        {
            Hit_record rec;
            float min = 0;
            float max = float.MaxValue;
            if (world.Hit(ray, ref min, ref max, out rec))
            {
                Ray scattered = new Ray();
                Vector3 attenuation = Vector3.one;

                if (depth < 50 && rec.mat.Scatter(ref ray, ref rec, ref attenuation, ref scattered))
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

        [MenuItem("Raytracing/Chapter10/1")]
        public static void Main()
        {
            int nx = 1280;
            int ny = 640;
            int ns = 16;

            RayCamera camera = new RayCamera();
            float R = Mathf.Cos(Mathf.PI / 4);
            HitList list = new HitList();
            list.Add(new Sphere(new Vector3(-R, 0, -1), R, new Lambertian(new Vector3(0f, 0f, 1f))));
            list.Add(new Sphere(new Vector3(R, 0, -1), R, new Lambertian(new Vector3(1f, 0f, 0f))));

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

            ImageHelper.SaveImg(tex, "Img/chapter10_1.png");
            Debug.Log("Chapter 10_1 done");
        }


        [MenuItem("Raytracing/Chapter10/2")]
        public static void Main2()
        {
            int nx = 1280;
            int ny = 640;
            int ns = 16;
            Vector3 lookfrom = new Vector3(-2, 2, 1);
            Vector3 lookat = new Vector3(0, 0, -1);
            Vector3 up = new Vector3(0, 1, 0);
            RayCamera camera = new RayCamera(lookfrom,lookat,up,90,(float)(nx) / (float)(ny));

            HitList list = new HitList();
            list.Add(new Sphere(new Vector3(0, 0, -1), 0.5f, new Lambertian(new Vector3(0.1f, 0.2f, 0.5f))));
            list.Add(new Sphere(new Vector3(0, -100.5f, -1), 100, new Lambertian(new Vector3(0.8f, 0.8f, 0.0f))));
            list.Add(new Sphere(new Vector3(1, 0, -1), 0.5f, new Metal(new Vector3(0.8f, 0.6f, 0.2f), 0.3f)));
            list.Add(new Sphere(new Vector3(-1, 0, -1), 0.5f, new DielectricShlick(1.5f)));
            list.Add(new Sphere(new Vector3(-1, 0, -1), -0.45f, new DielectricShlick(1.5f)));
            Texture2D tex = ImageHelper.CreateImg(nx, ny);

            for (int j = ny - 1; j >= 0; --j)
            {
                for (int i = 0; i < nx; ++i)
                {
                    Vector3 color = Vector3.zero;
                    for (int k = 0; k < ns; ++k)
                    {
                        float u = (float)(i + Random.Range(0f, 1f)) / (float)(nx);
                        float v = (float)(j + Random.Range(0f, 1f)) / (float)(ny);

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

            ImageHelper.SaveImg(tex, "Img/chapter10_2.png");
            Debug.Log("Chapter 10_2 done");
        }


        [MenuItem("Raytracing/Chapter10/3")]
        public static void Main3()
        {
            int nx = 1280;
            int ny = 640;
            int ns = 16;
            Vector3 lookfrom = new Vector3(-2, 2, 1);
            Vector3 lookat = new Vector3(0, 0, -1);
            Vector3 up = new Vector3(0, 1, 0);
            RayCamera camera = new RayCamera(lookfrom, lookat, up, 35, (float)(nx) / (float)(ny));

            HitList list = new HitList();
            list.Add(new Sphere(new Vector3(0, 0, -1), 0.5f, new Lambertian(new Vector3(0.1f, 0.2f, 0.5f))));
            list.Add(new Sphere(new Vector3(0, -100.5f, -1), 100, new Lambertian(new Vector3(0.8f, 0.8f, 0.0f))));
            list.Add(new Sphere(new Vector3(1, 0, -1), 0.5f, new Metal(new Vector3(0.8f, 0.6f, 0.2f), 0.3f)));
            list.Add(new Sphere(new Vector3(-1, 0, -1), 0.5f, new DielectricShlick(1.5f)));
            list.Add(new Sphere(new Vector3(-1, 0, -1), -0.45f, new DielectricShlick(1.5f)));
            Texture2D tex = ImageHelper.CreateImg(nx, ny);

            for (int j = ny - 1; j >= 0; --j)
            {
                for (int i = 0; i < nx; ++i)
                {
                    Vector3 color = Vector3.zero;
                    for (int k = 0; k < ns; ++k)
                    {
                        float u = (float)(i + Random.Range(0f, 1f)) / (float)(nx);
                        float v = (float)(j + Random.Range(0f, 1f)) / (float)(ny);

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

            ImageHelper.SaveImg(tex, "Img/chapter10_3.png");
            Debug.Log("Chapter 10_3 done");
        }

    }
}