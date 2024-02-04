using System.Collections;
using System.Collections.Generic;
using TrackVisuUtils;
using UnityEngine;

namespace TrackVisuMeshMaker
{
    struct MeshMaker
    {
        List<Vector3> vertices;
        List<int> tris;
        int index;

        List<GameObject> meshes_gameobjects;
        MeshFilter mesh_prefab;
        Transform parent;
        int cross_fidelity;
        bool support_reuse_of_vertices;

        bool reuse_vertices;

        public MeshMaker(List<GameObject> meshes_gameobjects, MeshFilter mesh_prefab, Transform parent, int cross_fidelity, bool segments_will_be_consecutive)
        {
            vertices = new List<Vector3>();
            tris = new List<int>();
            index = 0;

            this.meshes_gameobjects = meshes_gameobjects;
            this.mesh_prefab = mesh_prefab;
            this.parent = parent;
            this.cross_fidelity = cross_fidelity;
            this.support_reuse_of_vertices = segments_will_be_consecutive;

            reuse_vertices = false;
        }

        internal int Add(Vector3 pos)
        {
            vertices.Add(pos);
            tris.Add(index++);
            return index - 1;
        }
        internal int Add(Vector3 pos, float inversed_z)
        {
            pos.z = -inversed_z;
            vertices.Add(pos);
            tris.Add(index++);
            return index - 1;
        }
        internal int AddExisting(int existing_vertex_index)
        {
            tris.Add(existing_vertex_index);
            return existing_vertex_index;
        }
        internal int AddIfNotExisting(Vector3 pos, float inversed_z, int potentially_existing_vertex_index)
        {
            if (reuse_vertices && support_reuse_of_vertices)
                potentially_existing_vertex_index = FindVertextToReuse(pos, potentially_existing_vertex_index);
            else
                potentially_existing_vertex_index = -1;

            if (potentially_existing_vertex_index < 0)
                return Add(pos, inversed_z);
            else
                return AddExisting(potentially_existing_vertex_index);
        }

        private float GetAbsolutePerlin(float x, float y, float noise_factor, float perlin_scale)
        {
            return (1f - noise_factor) + (noise_factor * Mathf.PerlinNoise(Mathf.PingPong(x / perlin_scale, 1f), Mathf.PingPong(y / perlin_scale, 1f)));
        }

        internal void AddSegmentWithCurve(
            Vector3 right_pos, Vector3 next_right_pos,
            AnimationCurve curve, float dist_to_left, float height,
            float perlin_noise_factor = 0f, float perlin_noise_scale = 1f)
        {
            Vector3 from_right_to_left_fragment = new Vector3(0, dist_to_left / cross_fidelity, 0);

            int previous_stripe_left_pos_ind = -1;
            int previous_stripe_next_left_pos_ind = -1;

            bool inverted = dist_to_left > 0;

            for (int i = 0; i < cross_fidelity; i++)
            {
                float stripe_factor_right = (i / (float)cross_fidelity);
                float stripe_factor_left = ((i + 1) / (float)cross_fidelity);

                float base_z_right = height * curve.Evaluate(stripe_factor_right);
                float base_z_left = height * curve.Evaluate(stripe_factor_left);

                Vector3 left_pos = right_pos + from_right_to_left_fragment;
                Vector3 next_left_pos = next_right_pos + from_right_to_left_fragment;

                float z_right = base_z_right;
                float z_next_right = base_z_right;
                float z_left = base_z_left;
                float z_next_left = base_z_left;

                if (i == 0)
                {
                    if (perlin_noise_factor > 0f)
                    {
                        z_right *= GetAbsolutePerlin(right_pos.x, right_pos.y, perlin_noise_factor, perlin_noise_scale);
                        z_next_right *= GetAbsolutePerlin(next_right_pos.x, next_right_pos.y, perlin_noise_factor, perlin_noise_scale);
                        z_left *= GetAbsolutePerlin(left_pos.x, left_pos.y, perlin_noise_factor, perlin_noise_scale);
                        z_next_left *= GetAbsolutePerlin(next_left_pos.x, next_left_pos.y, perlin_noise_factor, perlin_noise_scale);
                    }

                    int relative_index = vertices.Count - cross_fidelity;

                    previous_stripe_left_pos_ind = AddIfNotExisting(left_pos, z_left, relative_index - 1);
                    int right_pos_ind = AddIfNotExisting(right_pos, z_right, relative_index);
                    previous_stripe_next_left_pos_ind = Add(next_left_pos, z_next_left);

                    AddExisting(previous_stripe_next_left_pos_ind);
                    AddExisting(right_pos_ind);
                    Add(next_right_pos, z_next_right);
                }
                else
                {
                    if (perlin_noise_factor > 0f)
                    {
                        z_left *= GetAbsolutePerlin(left_pos.x, left_pos.y, perlin_noise_factor, perlin_noise_scale);
                        z_next_left *= GetAbsolutePerlin(next_left_pos.x, next_left_pos.y, perlin_noise_factor, perlin_noise_scale);
                    }

                    int existing_right_pos_ind = previous_stripe_left_pos_ind;
                    int existing_next_right_pos_ind = previous_stripe_next_left_pos_ind;

                    previous_stripe_left_pos_ind = AddIfNotExisting(left_pos, z_left, vertices.Count - cross_fidelity - 1);
                    AddExisting(existing_right_pos_ind);
                    previous_stripe_next_left_pos_ind = Add(next_left_pos, z_next_left);

                    AddExisting(previous_stripe_next_left_pos_ind);
                    AddExisting(existing_right_pos_ind);
                    AddExisting(existing_next_right_pos_ind);
                }

                if (inverted)
                    InvertLastTwoTriangles();

                /*
                /// Simple version with no reusage of indicies (results in jagged shading)
                Add(right_pos + from_right_to_left_fragment, left_z);
                Add(right_pos, right_z);
                Add(next_right_pos + from_right_to_left_fragment, left_z);

                Add(next_right_pos + from_right_to_left_fragment, left_z);
                Add(right_pos, right_z);
                Add(next_right_pos, right_z);
                */

                right_pos += from_right_to_left_fragment;
                next_right_pos += from_right_to_left_fragment;
            }

            reuse_vertices = true;
        }

        private int FindVertextToReuse(Vector3 pos, int expected)
        {
            int last = -1;

            if (vertices[expected].x == pos.x && vertices[expected].y == pos.y)
                return expected;

            for (int i = vertices.Count - 1; i >= 0; i--)
                if (vertices[i].x == pos.x && vertices[i].y == pos.y)
                    last = i;

            return last;
        }

        private void InvertLastTwoTriangles()
        {
            int offset = tris.Count;

            int tri_6 = tris[offset - 6];
            int tri_4 = tris[offset - 4];
            int tri_3 = tris[offset - 3];
            int tri_1 = tris[offset - 1];

            tris[offset - 6] = tri_4;
            tris[offset - 4] = tri_6;

            tris[offset - 3] = tri_1;
            tris[offset - 1] = tri_3;
        }

        internal void InstantiateMesh(string name, Material material)
        {
            if (index == 0)
                return;

            MeshFilter mesh_prefab_local = mesh_prefab;
            MeshFilter mesh_filter = GlobalObjectPool.Acquire("TrackMesh", () => Object.Instantiate(mesh_prefab_local), parent);
            mesh_filter.transform.localPosition = Vector3.zero;
            mesh_filter.transform.localRotation = Quaternion.identity;
            mesh_filter.gameObject.layer = parent.gameObject.layer;

            Mesh mesh = mesh_filter.sharedMesh;
            if (mesh != null && mesh)
                Object.Destroy(mesh);

            mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, vertices);
            mesh.SetIndices(tris, MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.RecalculateNormals();

            MeshRenderer renderer = mesh_filter.GetComponent<MeshRenderer>();

            renderer.material = material;

            mesh_filter.sharedMesh = mesh;
            meshes_gameobjects.Add(mesh_filter.gameObject);

            mesh_filter.gameObject.name = name;
        }

        // Generates UVs in such a way that the whole mesh is covered by the texture exactly once in each dimension.
        private List<Vector2> GenerateOneToOneUVs(List<Vector3> vertices)
        {
            List<Vector2> uvs = new List<Vector2>();

            float min_x = float.MaxValue;
            float min_y = float.MaxValue;
            float max_x = float.MinValue;
            float max_y = float.MinValue;
            int len = vertices.Count;
            for (int i = 0; i < len; i++)
            {
                Vector3 vert = vertices[i];
                min_x = Mathf.Min(min_x, vert.x);
                min_y = Mathf.Min(min_y, vert.y);
                max_x = Mathf.Max(max_x, vert.x);
                max_y = Mathf.Max(max_y, vert.y);
            }

            Vector2 factor = new Vector2(1f / (max_x - min_x), 1f / (max_y - min_y));
            for (int i = 0; i < len; i++)
            {
                Vector3 vert = vertices[i];

                uvs.Add(new Vector2(
                    (vert.x - min_x) * factor.x,
                    (vert.y - min_y) * factor.y
                    ));
            }

            return uvs;
        }

        internal void InstantiateAndReset(string name, Material material)
        {
            if (index == 0)
                return;

            InstantiateMesh(name, material);

            vertices = new List<Vector3>();
            tris = new List<int>();
            index = 0;
        }

        internal void ResetVertexReusage()
        {
            reuse_vertices = false;
        }
    }
}