using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Collections.Generic;

namespace Chapter12
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
        bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered,System.Random seed);
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

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered,System.Random seed)
        {
            Vector3 target = rec.normal.normalized * 0.5f +
                new Vector3(Chapter12.RandomFloat11(seed), Chapter12.RandomFloat11(seed), Chapter12.RandomFloat11(seed)).normalized;

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

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered, System.Random seed)
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

        public Metal(Vector3 a, float f)
        {
            albedo = a;
            fuzz = f;
        }

        private Vector3 Random_in_unit_sphere(System.Random seed)
        {
            return new Vector3(Chapter12.RandomFloat11(seed), Chapter12.RandomFloat11(seed), Chapter12.RandomFloat11(seed)).normalized;
        }

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered, System.Random seed)
        {
            Vector3 reflected = Vector3.Reflect(r.direction.normalized, rec.normal.normalized);
            reflected = reflected + fuzz * Random_in_unit_sphere(seed);

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

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered, System.Random seed)
        {
            Vector3 outward_normal = Vector3.zero;
            Vector3 reflected = Vector3.Reflect(r.direction.normalized, rec.normal.normalized);
            float ni_over_nt = 0f;
            attenuation.x = 1.0f;
            attenuation.y = 1.0f;
            attenuation.z = 1.0f;
            Vector3 refracted;
            if (Vector3.Dot(r.direction, rec.normal) > 0)
            {
                outward_normal = -rec.normal;
                ni_over_nt = ref_idx;
            }
            else
            {
                outward_normal = rec.normal;
                ni_over_nt = 1.0f / ref_idx;
            }

            if (Refract(r.direction, outward_normal, ni_over_nt, out refracted))
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

        private float Schlick(float cosine, float idx)
        {
            float r0 = (1.0f - idx) / (1 + idx);
            r0 *= r0;
            return r0 + (1 - r0) * Mathf.Pow((1 - cosine), 5);
        }

        public bool Scatter(ref Ray r, ref Hit_record rec, ref Vector3 attenuation, ref Ray scattered, System.Random seed)
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
            if (Chapter12.RandomFloat01(seed) < reflect_prob)
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

    public class Chapter12
    {
        private static Vector3 topColor = Vector3.one;
        private static Vector3 bottomColor = new Vector3(0.5f, 0.9f, 1.0f);

        private static System.Random gSeed = new System.Random();
        //private static readonly object locker = new object();

        private static int myThreadCount = 0;

        public class Param
        {
            public Hitable world;
            public MotionBlurRayCamera camera;
            public int pos_x;
            public int pos_y;
            public int img_width;
            public int img_height;
            public int times;
            public Vector3[,] img;
            public ManualResetEvent mre;
            public System.Random seed;
        }

        public static void ThreadRayMain(System.Object obj)
        {
            Param parma = (Param)obj;
            DoRaycastInThread(parma.world, parma.camera, parma.pos_x, parma.pos_y,
                        parma.img_width, parma.img_height, parma.times, parma.img,parma.mre,parma.seed);
        }

        public static void DoRaycastInThread(Hitable world, MotionBlurRayCamera camera,
                                                int i, int j, int nx, float ny, int ns, Vector3[,] img,ManualResetEvent mre,System.Random seed)
        {
            Vector3 color = Vector3.zero;
            for (int k = 0; k < ns; ++k)
            {
                float u = (float)(i + RandomFloat01(seed)) / (float)(nx);
                float v = (float)(j + RandomFloat01(seed)) / (float)(ny);
                Ray r = camera.GetRay(u, v,seed);
                color += RayCast(r, world,seed,0);
            }
            color = color / (float)(ns);
            color.x = Mathf.Sqrt(color.x);
            color.y = Mathf.Sqrt(color.y);
            color.z = Mathf.Sqrt(color.z);

            //lock (img)
            {                
                img[i, j] = color;
            }

            if (Interlocked.Decrement(ref myThreadCount) == 0)
            {
                mre.Set();
            }
            
        }

        public static Vector3 RayCast(Ray ray, Hitable world,System.Random seed, int depth)
        {
            Hit_record rec;
            float min = 0;
            float max = float.MaxValue;
            if (world.Hit(ray, ref min, ref max, out rec))
            {
                Ray scattered = new Ray();
                Vector3 attenuation = Vector3.one;

                if (depth < 50 && rec.mat.Scatter(ref ray, ref rec, ref attenuation, ref scattered,seed))
                {
                    var color = RayCast(scattered, world, seed,depth + 1);
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

        public static float RandomFloat01(System.Random random)
        {
            //lock (locker)
            //{
            //    return (float)(random.NextDouble());
            //}
            return (float)(random.NextDouble() * 2.0f - 1.0f);
        }

        public static float RandomFloat11(System.Random random)
        {
            //lock (locker)
            //{
            //    return (float)(random.NextDouble() * 2.0f - 1.0f);
            //}
            return (float)(random.NextDouble() * 2.0f - 1.0f);
        }

        private static void RandomScene(ref HitList list)
        {
            list.Add(new Sphere(new Vector3(0, -1000, 0), 1000f, new Lambertian(new Vector3(0.5f, 0.5f, 0.5f))));
            for (int i = -11; i < 11; ++i)
            {
                for (int j = -11; j < 11; ++j)
                {
                    Vector3 center = new Vector3(i + 0.9f * RandomFloat01(gSeed), 0.2f, j + 0.9f * RandomFloat01(gSeed));
                    Vector3 baseCenter = new Vector3(4, 0.2f, 0);
                    float choose_mat = RandomFloat01(gSeed);
                    if ((center - baseCenter).magnitude > 0.9)
                    {
                        if (choose_mat < 0.5f)
                        {
                            list.Add(new Sphere(center, 0.2f, new Lambertian(new Vector3(RandomFloat01(gSeed) * RandomFloat01(gSeed),
                                        RandomFloat01(gSeed) * RandomFloat01(gSeed), RandomFloat01(gSeed) * RandomFloat01(gSeed)))));
                        }
                        else if (choose_mat < 0.75f)
                        {
                            list.Add(new Sphere(center, 0.2f, new Metal(new Vector3(0.5f * (1 + RandomFloat01(gSeed)),
                                                                                  0.5f * (1 + RandomFloat01(gSeed)),
                                                                                  0.5f * (1 + RandomFloat01(gSeed))), 0.5f * RandomFloat01(gSeed))));
                        }
                        else
                        {
                            list.Add(new Sphere(center, 0.2f, new Dielectric(1.5f)));
                        }
                    }
                }
            }

            list.Add(new Sphere(new Vector3(0, 1, 0), 1f, new Dielectric(1.5f)));
            list.Add(new Sphere(new Vector3(-4, 1, 0), 1f, new Lambertian(new Vector3(0.4f, 0.2f, 0.1f))));
            list.Add(new Sphere(new Vector3(4, 1, 0), 1f, new Metal(new Vector3(0.7f, 0.6f, 0.5f), 0.0f)));
        }

        [MenuItem("Raytracing/Chapter12/SingleThread(Slow)")]
        public static void Main()
        {
            int nx = 640;
            int ny = 480;
            int ns = 32;
    
            Vector3 lookfrom = new Vector3(13, 5, 3);
            Vector3 lookat = new Vector3(0, 0, 0);
            float dist_to_focus = 10;
            float aperture = 0.1f;
            Vector3 up = new Vector3(0, 1, 0);
            MotionBlurRayCamera camera = new MotionBlurRayCamera(lookfrom, lookat, up, 20,
                                        (float)(nx) / (float)(ny), aperture, dist_to_focus);

            HitList list = new HitList();
            RandomScene(ref list);
            Texture2D tex = ImageHelper.CreateImg(nx, ny);

            for (int j = ny - 1; j >= 0; --j)
            {
                for (int i = 0; i < nx; ++i)
                {
                    Vector3 color = Vector3.zero;
                    for (int k = 0; k < ns; ++k)
                    {
                        float u = (float)(i + RandomFloat01(gSeed)) / (float)(nx);
                        float v = (float)(j + RandomFloat01(gSeed)) / (float)(ny);

                        Ray r = camera.GetRay(u, v);
                        color += RayCast(r, list,gSeed, 0);
                    }
                    color = color / (float)(ns);
                    color.x = Mathf.Sqrt(color.x);
                    color.y = Mathf.Sqrt(color.y);
                    color.z = Mathf.Sqrt(color.z);
                    ImageHelper.SetPixel(tex, i, j, color);
                }
            }

            ImageHelper.SaveImg(tex, "Img/chapter12_single.png");
            Debug.Log("Chapter 12 done");
        }


        [MenuItem("Raytracing/Chapter12/MutiThread")]
        public static void Test()
        {
            var b = ThreadPool.SetMaxThreads(System.Environment.ProcessorCount, System.Environment.ProcessorCount);
            //Debug.Log(b);

            int nx = 1280;
            int ny = 720;
            int ns = 128;

            myThreadCount = nx * ny ;

            Vector3 lookfrom = new Vector3(13, 3, 3);
            Vector3 lookat = new Vector3(0, 0, 0);
            float dist_to_focus = 10;
            float aperture = 0.1f;
            Vector3 up = new Vector3(0, 1, 0);
            MotionBlurRayCamera camera = new MotionBlurRayCamera(lookfrom, lookat, up, 20,
                                        (float)(nx) / (float)(ny), aperture, dist_to_focus);

            HitList list = new HitList();
            RandomScene(ref list);
            Texture2D tex = ImageHelper.CreateImg(nx, ny);
            Vector3[,] imgBin = new Vector3[nx, ny];

            ManualResetEvent resetEvent = new ManualResetEvent(false);
           
            for (int j = ny - 1; j >= 0; --j)
            {
                for (int i = 0; i < nx; ++i)
                {
                    var param = new Param();
                    param.world = list;
                    param.camera = camera;
                    param.pos_x = i;
                    param.pos_y = j;
                    param.img_width = nx;
                    param.img_height = ny;
                    param.times = ns;
                    param.img = imgBin;
                    param.mre = resetEvent;
                    param.seed = new System.Random();

                    while (!ThreadPool.QueueUserWorkItem(ThreadRayMain, param))
                    {
                        Debug.Log("wait ");
                        Thread.Sleep(500);
                    }
                }
            }
            resetEvent.WaitOne();            

            for (int j = ny - 1; j >= 0; --j)
            {
                for (int i = 0; i < nx; ++i)
                {
                    ImageHelper.SetPixel(tex, i, j, imgBin[i, j]);
                }
            }
            ImageHelper.SaveImg(tex, "Img/chapter12.png");
            Debug.Log("Chapter 12 done");
        }
    }
}