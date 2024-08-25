using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace AiWorldGeneration
{
    public class SkyboxImporter
    {

        [Tooltip("Relative file path for the generated skybox")]
        [SerializeField]
        private string dstImagePath = "Assets/Textures/new.png";

        /// <summary>
        /// Imports an image from the specified source path and saves it to the specified destination path.
        /// </summary>
        /// <param name="srcImagePath">The source path of the image to be imported.</param>
        /// <returns>The relative path of the imported image from the project root directory.</returns>
        public string ImportImage(string srcImagePath)
        {
            // Absolute path for the destination image
            var dst = Application.dataPath + "/../" + dstImagePath;
            File.Copy(srcImagePath, dst, true);
            return Path.GetRelativePath(Application.dataPath + "/..", dst);
        }

        /// <summary>
        /// Converts the specified image path to a cubemap texture.
        /// </summary>
        /// <param name="imagePath">The path of the image to be converted to a cubemap.</param>
        /// <returns>The cubemap texture created from the specified image.</returns>
        public Cubemap ImportToCubemap(string imagePath)
        {
#if UNITY_EDITOR
            // Load the asset to Unity
            AssetDatabase.ImportAsset(imagePath, ImportAssetOptions.ForceUpdate);

            // Edit the new asset
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);

            importer.textureShape = TextureImporterShape.TextureCube;
            importer.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;

            Debug.Log("Saving under " + imagePath);

            return AssetDatabase.LoadAssetAtPath<Cubemap>(imagePath);
#else
        throw new System.InvalidOperationException("Cannot import skyboxes at runtime!");
#endif
        }
    }
}
