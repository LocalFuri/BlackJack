using UnityEngine;

namespace Blackjack
{
    /// <summary>
    /// Builds a subdivided card quad mesh with sprite UVs for MeshRenderer deformation.
    /// </summary>
    public static class SpriteCardMeshBuilder
    {
        public static Mesh CreateGridMesh(Sprite sprite, int columns, int rows, float worldWidth, float worldHeight)
        {
            var mesh = new Mesh { name = "SpriteCardMesh" };

            int vertCount = (columns + 1) * (rows + 1);
            var vertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[columns * rows * 6];

            Rect texRect = sprite != null ? sprite.textureRect : new Rect(0, 0, 1, 1);
            Texture2D tex = sprite != null ? sprite.texture : null;
            float texW = tex != null ? tex.width : 1f;
            float texH = tex != null ? tex.height : 1f;
            float u0 = texRect.x / texW;
            float u1 = (texRect.x + texRect.width) / texW;
            float v0 = texRect.y / texH;
            float v1 = (texRect.y + texRect.height) / texH;

            float halfW = worldWidth * 0.5f;
            float halfH = worldHeight * 0.5f;

            int vi = 0;
            for (int y = 0; y <= rows; y++)
            {
                float ty = y / (float)rows;
                float py = Mathf.Lerp(-halfH, halfH, ty);
                float tv = Mathf.Lerp(v0, v1, ty);

                for (int x = 0; x <= columns; x++)
                {
                    float tx = x / (float)columns;
                    float px = Mathf.Lerp(-halfW, halfW, tx);
                    float tu = Mathf.Lerp(u0, u1, tx);

                    vertices[vi] = new Vector3(px, py, 0f);
                    uvs[vi] = new Vector2(tu, tv);
                    vi++;
                }
            }

            int ti = 0;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int bl = y * (columns + 1) + x;
                    int br = bl + 1;
                    int tl = bl + (columns + 1);
                    int tr = tl + 1;

                    triangles[ti++] = bl;
                    triangles[ti++] = tl;
                    triangles[ti++] = tr;
                    triangles[ti++] = bl;
                    triangles[ti++] = tr;
                    triangles[ti++] = br;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static void CopyVertices(Mesh mesh, Vector3[] buffer)
        {
            if (mesh == null || buffer == null || buffer.Length != mesh.vertexCount)
                return;

            System.Array.Copy(mesh.vertices, buffer, buffer.Length);
        }

        public static void ApplyVertices(Mesh mesh, Vector3[] vertices)
        {
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }
    }
}
