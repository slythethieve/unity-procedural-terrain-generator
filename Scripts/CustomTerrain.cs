using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;


[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour
{

    public Vector2 randomHeightRange = new Vector2(0, 0.1f);
    public Texture2D heightMapImage;
    public Vector3 heightMapScale = new Vector3(1, 1, 1);
    public bool resetTerrain = true;

    // Perlin noise stuff -------------------------------------------------------------------------------------------------------------------------------------------------
    public float perlinXScale = 0.01f;
    public float perlinYScale = 0.01f;
    public int perlinOffsetX = 0;
    public int perlinOffsetY = 0;
    public int perlinOctaves = 3;
    public float perlinPersistance = 8;
    public float perlinHeightScale = 0.09f;

    // Multiple Perlin noise stuff --------------------------------------------------------------------------------------------------------------------------------------------
    [System.Serializable]
    public class PerlinParameters
    {
        public float mPerlinXScale = 0.01f;
        public float mPerlinYScale = 0.01f;
        public int mPerlinOctaves = 3;
        public float mPerlinPersistance = 8;
        public float mPerlinHeightScale = 0.09f;
        public int mPerlinOffsetX = 0;
        public int mPerlinOffsetY = 0;
        public bool remove = false;
    }

    public List<PerlinParameters> perlinParameters = new List<PerlinParameters>
    {
        // We have to have at least one empty line otherwise the GUI package is going to complain and throw a null reference exception. 
        new PerlinParameters()
    };

    // Splatmaps -----------------------------------------------------------------------------------------------------------------------------------------------------------------
    [System.Serializable]
    public class SplatHeights
    {
        public Texture2D texture = null;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 1.5f;
        public Vector2 tileOffset = new Vector2(0, 0); // how far across the tile you want to start before you paste it over the surface
        public Vector2 tileSize = new Vector2(50, 50); // how big you want to tile over the surface
        public bool remove = false;
    }
    public List<SplatHeights> splatHeights = new List<SplatHeights>()
    {
        new SplatHeights()
    };

    public float splatOffset = 0.1f;
    public float splatNoiseXScale = 0.01f;
    public float splatNoiseYScale = 0.01f;
    public float splatNoiseScaler = 0.2f;

    // Vegetation stuff -----------------------------------------------------------------------------------------------------------------------------------------------------------
    [System.Serializable]
    public class Vegetation
    {
        public GameObject Mesh;
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 90;
        public float minScale = 0.5f;
        public float maxScale = 1.0f;
        public Color color1 = Color.white;
        public Color color2 = Color.white;
        public Color lightColor = Color.white;
        public float minRotation = 0;
        public float maxRotation = 360;
        public float density = 0.5f;
        public bool remove = false;
    }
    public List<Vegetation> vegetation = new List<Vegetation>()
    {
        new Vegetation()
    };
    public int maxTrees = 10000;
    public int treeSpacing = 5;

    // Detail Stuff ---------------------------------------------------------------------------------------------------------------------------------------------------------------
    [System.Serializable]
    public class Detail
    {
        public GameObject prototype = null; 
        public Texture2D prototypeTexture = null;  
        public float minHeight = 0.1f;
        public float maxHeight = 0.2f;
        public float minSlope = 0;
        public float maxSlope = 1;
        public Color dryColor = Color.white;
        public Color healthyColor = Color.white;
        public Vector2 heightRange = new Vector2(1, 1);
        public Vector2 widthRange = new Vector2(1, 1);
        public float noiseSpread = 0.5f;
        public float overlap = 0.01f; 
        public float feather = 0.05f; 
        public float density = 0.5f;
        public bool remove = false;
    }
    public List<Detail> details = new List<Detail>()
    {
        new Detail()
    };
    public int maxDetails = 5000;
    public int detailSpacing = 5;

    // Water Level stuff ---------------------------------------------------------------------------------------------------------------------------------------------------------
    public float waterHeight = 0.5f;
    public GameObject waterGO;
    public Material shoreLineMaterial;


    // Erosion stuff--------------------------------------------------------------------------------------------------------------------------------------------------------------
    public enum ErosionType { Rain = 0, Thermal = 1, Tidal = 2, River = 3 , Wind = 4, Canyon = 5}
    public ErosionType erosionType = ErosionType.Rain;
    public float erosionStrength = 0.1f;
    public int springsPerRiver = 5;
    public float solubility = 0.01f;
    public int droplets = 10;
    public int erosionSmoothAmount = 5;
    public float erosionHeightScalar = 0.01f;


    // Voronoi stuff -------------------------------------------------------------------------------------------------------------------------------------------------------------
    public float voronoiFallOff = 0.2f;
    public float voronoiDropOff = 0.6f;
    public float voronoiMinHeight = 0.1f;
    public float voronoiMaxHeight = 0.6f;
    public int voronoiPeaks = 5;
    public enum VoronoiType { Linear = 0, Power = 1, Combined = 2, Mushroom = 3};
    public VoronoiType voronoiType = VoronoiType.Linear; // By default we set it to be linear

    // Midpoint Displacement stuff --------------------------------------------------------------------------------------------------------------------------------------------------
    public float MPDheightMin = -2f;
    public float MPDheightMax = 2f;
    public float MPDheightDampenerPower = 2.0f;
    public float MPDroughness = 2.0f;

    // Cloud stuff ------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public int numClouds = 1;
    public int particlesPerCloud = 50;
    public Vector3 cloudScaleMin = new Vector3(1, 1, 1);
    public Vector3 cloudScaleMax = new Vector3(1, 1, 1);
    public Material cloudMaterial;
    public Material cloudShadowMaterial;
    public float cloudStartSize = 5;
    public Color cloudColor = Color.white;
    public Color cloudLining = Color.grey;
    public float cloudMinSpeed = 0.2f;
    public float cloudMaxSpeed = 0.5f;
    public float cloudRange = 500.0f;

    // Smooth stuff -----------------------------------------------------------------------------------------------------------------------------------------------------------------
    public int smoothAmount = 2;
    

    public Terrain terrain;
    public TerrainData terrainData;


    public void GenerateClouds()
    {
        GameObject cloudManager = GameObject.Find("CloudManager");
        if (!cloudManager)
        {
            cloudManager = new GameObject();
            cloudManager.name = "CloudManager";
            cloudManager.AddComponent<CloudManager>();
            cloudManager.transform.position = this.transform.position;
        }

        GameObject[] allClouds = GameObject.FindGameObjectsWithTag("Cloud");
        for(int i = 0; i < allClouds.Length; i++)
        {
            DestroyImmediate(allClouds[i]);
        }
        
        for (int c = 0; c < numClouds; c++)
        {
            GameObject cloudGO = new GameObject();
            cloudGO.name = "Cloud" + c;
            cloudGO.tag = "Cloud";

            cloudGO.transform.rotation = cloudManager.transform.rotation;
            cloudGO.transform.position = cloudManager.transform.position;

            CloudController cc = cloudGO.AddComponent<CloudController>();
            cc.lining = cloudLining;
            cc.color = cloudColor;
            cc.numberOfParticles = particlesPerCloud;
            cc.minSpeed = cloudMinSpeed;
            cc.maxSpeed = cloudMaxSpeed;
            cc.distance = cloudRange;

            
            ParticleSystem cloudSystem = cloudGO.AddComponent<ParticleSystem>();
            Renderer cloudRenderer = cloudGO.GetComponent<Renderer>();
            cloudRenderer.material = cloudMaterial;

            
            cloudGO.layer = LayerMask.NameToLayer("Sky");
            GameObject cloudProjector = new GameObject();
            cloudProjector.name = "Shadow";
            cloudProjector.transform.position = cloudGO.transform.position;
            cloudProjector.transform.forward = Vector3.down;
            cloudProjector.transform.parent = cloudGO.transform;

            if (UnityEngine.Random.Range(0,10) < 5) 
            {
                Projector cp = cloudProjector.AddComponent<Projector>(); 
                cp.material = cloudShadowMaterial;
                cp.farClipPlane = terrainData.size.y;
                int skyLayerMask = 1 << LayerMask.NameToLayer("Sky");
                int waterLayerMask = 1 << LayerMask.NameToLayer("Water");
                cp.ignoreLayers = skyLayerMask | waterLayerMask; 
                                                                 
                cp.fieldOfView = 20.0f;
            }
            

            cloudRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; 
            cloudRenderer.receiveShadows = false;  

            
            ParticleSystem.MainModule main = cloudSystem.main;
            main.loop = false;
            main.startLifetime = Mathf.Infinity; 
            main.startSpeed = 0;
            main.startSize = cloudStartSize;
            main.startColor = Color.white;

            
            var emission = cloudSystem.emission;
            emission.rateOverTime = 0; // all at once;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0.0f, (short)particlesPerCloud) }); 
            var shape = cloudSystem.shape;

            shape.shapeType = ParticleSystemShapeType.Sphere;
            Vector3 newScale = new Vector3(UnityEngine.Random.Range(cloudScaleMin.x, cloudScaleMax.x),
                                           UnityEngine.Random.Range(cloudScaleMin.y, cloudScaleMax.y),
                                           UnityEngine.Random.Range(cloudScaleMin.z, cloudScaleMax.z));
            shape.scale = newScale;

            cloudGO.transform.parent = cloudManager.transform;
            cloudGO.transform.localScale = new Vector3(1, 1, 1);

        }
    }


    public void Erode ()
    {
        if (erosionType == ErosionType.Rain)
            Rain();
        else if (erosionType == ErosionType.Tidal)
            Tidal();
        else if (erosionType == ErosionType.Thermal)
            Thermal();
        else if (erosionType == ErosionType.River)
            River();
        else if (erosionType == ErosionType.Wind)
            Wind();
        else if (erosionType == ErosionType.Canyon)
        {
            DigCanyon();
        }
        smoothAmount = erosionSmoothAmount;
        Smooth();
    }

    void Rain()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        for (int i = 0; i < droplets; i++)
        {
            heightMap[UnityEngine.Random.Range(0, terrainData.heightmapResolution), UnityEngine.Random.Range(0, terrainData.heightmapResolution)] -= erosionStrength;
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    void Tidal()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapResolution, terrainData.heightmapResolution);
                foreach (Vector2 n in neighbours)
                {
                    if (heightMap[x, y] < waterHeight && heightMap[(int)n.x, (int)n.y] > waterHeight)
                    {
                        heightMap[x, y] = waterHeight;
                        heightMap[(int)n.x, (int)n.y] = waterHeight;
                    }
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    
    void Thermal()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapResolution, terrainData.heightmapResolution);
                foreach(Vector2 n in neighbours)
                {
                    if (heightMap[x, y] > heightMap[(int)n.x, (int)n.y] + erosionStrength)
                    {
                        float currentHeight = heightMap[x, y];
                        heightMap[x, y] -= currentHeight * erosionHeightScalar;
                        heightMap[(int)n.x, (int)n.y] += currentHeight * erosionHeightScalar;
                    }
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    void River()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,] erosionMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution]; 
        for (int i = 0; i < droplets; i++)
        {
            Vector2 dropletsPosition = new Vector2(UnityEngine.Random.Range(0, terrainData.heightmapResolution),
                                                   UnityEngine.Random.Range(0, terrainData.heightmapResolution));
            erosionMap[(int)dropletsPosition.x, (int)dropletsPosition.y] = erosionStrength;
            for (int j = 0; j < springsPerRiver; j++)
            {
                erosionMap = RunRiver(dropletsPosition, heightMap, erosionMap, terrainData.heightmapResolution, terrainData.heightmapResolution);
            }
        }

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                if (erosionMap[x, y] > 0) 
                {
                    heightMap[x, y] -= erosionMap[x, y]; 
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    float [,] RunRiver(Vector3 dropletsPosition, float[,] heightMap, float[,] erosionMap, int width, int height) 
    {
        
        while (erosionMap[(int)dropletsPosition.x, (int)dropletsPosition.y] > 0)
        {
            List<Vector2> neighbours = GenerateNeighbours(dropletsPosition, width, height);
            neighbours.Shuffle(); 
            bool foundLower = false;
            foreach (Vector2 n in neighbours)
            {
                
                if (heightMap[(int)n.x, (int)n.y] < heightMap[(int)dropletsPosition.x, (int)dropletsPosition.y])
                {
                    
                    erosionMap[(int)n.x, (int)n.y] = erosionMap[(int)dropletsPosition.x, (int)dropletsPosition.y] - solubility;
                    dropletsPosition = n; 
                    foundLower = true;
                    break;
                }
            }
            
            if (!foundLower)
            {
                erosionMap[(int)dropletsPosition.x, (int)dropletsPosition.y] -= solubility;
            }
        }
        return erosionMap;
    }


    void Wind()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float windDir = 30; 
        float sinAngle = -Mathf.Sin(Mathf.Deg2Rad * windDir);
        float cosAngle = Mathf.Cos(Mathf.Deg2Rad * windDir);

        for (int y = - (height - 1) * 2; y <= height * 2; y += 10) 
        {
            for (int x = - (width - 1) * 2; x <= width * 2; x += 1)
            {
                float thisNoise = (float)Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 20 * erosionStrength;
                int nx = (int)x;
                int digy = (int)y + (int)thisNoise;
                int ny = (int)y + 5 + (int)thisNoise; 
                Vector2 digCoords = new Vector2(x * cosAngle - digy * sinAngle, digy * cosAngle + x * sinAngle);
                Vector2 pileCoords = new Vector2(nx * cosAngle - ny * sinAngle, ny * cosAngle + nx * sinAngle);


                if (!(pileCoords.x < 0 || pileCoords.x > (width - 1) || pileCoords.y < 0 || pileCoords.y > (height - 1) || (int)digCoords.x < 0 || 
                    (int)digCoords.x > (width -1) || (int)digCoords.y < 0 || (int)digCoords.y > (height -1))) 
                {
                    heightMap[(int)digCoords.x, (int)digCoords.y] -= 0.001f;
                    heightMap[(int)pileCoords.x, (int)pileCoords.y] += 0.001f;
                }
            } 
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    float[,] tempHeightMap;
    void DigCanyon()
    {
        float digDepth = 0.05f;
        float bankSlope = 0.001f;
        float maxDepth = 0;
        tempHeightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        int cx = 1;
        int cy = UnityEngine.Random.Range(10, terrainData.heightmapResolution - 10);
        while (cy >= 0 && cy < terrainData.heightmapResolution && cx > 0 && cx < terrainData.heightmapResolution)
        {
            CanyonCrawler(cx, cy, tempHeightMap[cx, cy] - digDepth, bankSlope, maxDepth);
            cx = cx + UnityEngine.Random.Range(1, 3);
            cy = cy + UnityEngine.Random.Range(-2, 3);
        }
        terrainData.SetHeights(0, 0, tempHeightMap);
    }

    void CanyonCrawler(int x, int y, float height, float slope, float maxDepth)
    {
        if (x < 0 || x >= terrainData.heightmapResolution) return; // off x range of map
        if (y < 0 || y >= terrainData.heightmapResolution) return; // off y range of map
        if (height <= maxDepth) return; // if hit lowest level
        if (tempHeightMap[x, y] <= height) return; // if run into lower elevation


        tempHeightMap[x, y] = height;

        // These "little" crawlers are going to dig the slope from the original point we dug through.
        CanyonCrawler(x + 1, y, height + UnityEngine.Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x - 1, y, height + UnityEngine.Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x + 1, y + 1, height + UnityEngine.Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x - 1, y + 1, height + UnityEngine.Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x, y - 1, height + UnityEngine.Random.Range(slope, slope + 0.01f), slope, maxDepth);
        CanyonCrawler(x, y + 1, height + UnityEngine.Random.Range(slope, slope + 0.01f), slope, maxDepth);
    }

    public void AddWater()
    {
        GameObject water = GameObject.Find("water");
        if (!water) // Test to see if there is already water
        {
            water = Instantiate(waterGO, this.transform.position, this.transform.rotation); 
            water.name = "water";
        }
        water.transform.position = this.transform.position + new Vector3(terrainData.size.x / 2, waterHeight * terrainData.size.y, terrainData.size.z / 2);
        water.transform.localScale = new Vector3(terrainData.size.x, 1, terrainData.size.z);
    }

    
    public void DrawShoreLine()
    {
        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        int quadCount = 0;
        //GameObject quads = new GameObject("Quads");
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                // find spot on shore
                Vector2 thisLocation = new Vector2(x, y);
                List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.heightmapResolution, terrainData.heightmapResolution);
                foreach (Vector2 n in neighbours)
                {
                    // If the current point we're looping through is below the water level and its neighbour is above the water level, then we create the shoreline
                    if (heightMap[x, y] < waterHeight && heightMap[(int)n.x, (int)n.y] > waterHeight)
                    {
                        
                        quadCount++;
                        
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        go.transform.localScale *= 10.0f; // Just to make it bigger. 

                        
                        go.transform.position = this.transform.position + new Vector3(y / (float)terrainData.heightmapResolution * terrainData.size.z,
                                                                                        waterHeight * terrainData.size.y, // since the waterheight value is between 0 and 1, we need to multiply it by the acutal coordinate of the terrain
                                                                                        x / (float)terrainData.heightmapResolution * terrainData.size.x);
                        go.transform.LookAt(new Vector3(n.y / (float)terrainData.heightmapResolution * terrainData.size.x,
                                                        waterHeight * terrainData.size.y,
                                                        n.x / (float)terrainData.heightmapResolution * terrainData.size.z));
                        go.transform.Rotate(90, 0, 0);
                        go.tag = "Shore";
                        
                    }
                }
            }
        }
        
        GameObject[] shoreQuads = GameObject.FindGameObjectsWithTag("Shore"); 
        MeshFilter[] meshFilters = new MeshFilter[shoreQuads.Length]; 
        for (int m = 0; m < shoreQuads.Length; m++) 
        {
            meshFilters[m] = shoreQuads[m].GetComponent<MeshFilter>();
        }
        CombineInstance[] combine = new CombineInstance[meshFilters.Length]; 
        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix; 
            meshFilters[i].gameObject.active = false;             i++;
        }
        GameObject currentShoreLine = GameObject.Find("ShoreLine");
        if (currentShoreLine)
        {
            DestroyImmediate(currentShoreLine);
        }

        GameObject shoreLine = new GameObject();
        shoreLine.name = "ShoreLine";
        shoreLine.AddComponent<WaveAnimation>();
        shoreLine.transform.position = this.transform.position;
        shoreLine.transform.rotation = this.transform.rotation;
        MeshFilter thisMF = shoreLine.AddComponent<MeshFilter>();
        thisMF.mesh = new Mesh();
        shoreLine.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
        MeshRenderer r = shoreLine.AddComponent<MeshRenderer>();
        r.sharedMaterial = shoreLineMaterial;
        for (int sQ = 0; sQ < shoreQuads.Length; sQ++)
        {
            DestroyImmediate(shoreQuads[sQ]);
        }
    }
    public void AddDetails()
    {
        DetailPrototype[] newDetailPrototypes;
        newDetailPrototypes = new DetailPrototype[details.Count];
        int dindex = 0;
        foreach(Detail d in details)
        {
            newDetailPrototypes[dindex] = new DetailPrototype();
            newDetailPrototypes[dindex].prototype = d.prototype;
            newDetailPrototypes[dindex].prototypeTexture = d.prototypeTexture;
            newDetailPrototypes[dindex].healthyColor = d.healthyColor;
            newDetailPrototypes[dindex].dryColor = d.dryColor;
            newDetailPrototypes[dindex].minHeight = d.heightRange.x;
            newDetailPrototypes[dindex].maxHeight = d.heightRange.y;
            newDetailPrototypes[dindex].minWidth = d.widthRange.x;
            newDetailPrototypes[dindex].maxWidth = d.widthRange.y;
            newDetailPrototypes[dindex].noiseSpread = d.noiseSpread;
            if(newDetailPrototypes[dindex].prototype) 
            {
                // Used for meshes
                newDetailPrototypes[dindex].usePrototypeMesh = true;
                newDetailPrototypes[dindex].renderMode = DetailRenderMode.VertexLit;
            }
            else // If there is no prototype, we assume there must be a texture 
            {
                // Used for billboards
                newDetailPrototypes[dindex].usePrototypeMesh = false;
                newDetailPrototypes[dindex].renderMode = DetailRenderMode.GrassBillboard;
            }
            dindex++;
        }
        terrainData.detailPrototypes = newDetailPrototypes;

        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];
            for (int y = 0; y < terrainData.detailHeight; y += detailSpacing)
            {
                for (int x = 0; x < terrainData.detailWidth; x += detailSpacing)
                {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > details[i].density) continue;

                   
                    int xHM = (int)(x / (float)terrainData.detailWidth * terrainData.heightmapResolution);
                    int yHM = (int)(y / (float)terrainData.detailHeight * terrainData.heightmapResolution);

                    float thisNoise = Utilis.Map(Mathf.PerlinNoise(x * details[i].feather, y * details[i].feather), 0, 1, 0.5f, 1);
                    float thisHeightStart = details[i].minHeight * thisNoise - details[i].overlap * thisNoise; 
                    float nextHeightStart = details[i].maxHeight * thisNoise + details[i].overlap * thisNoise; 
                    float thisHeight = heightMap[yHM, xHM];
                    float steepness = terrainData.GetSteepness(xHM / (float)terrainData.size.x, yHM / (float)terrainData.size.z); // Remember to normalize. 

                    if ((thisHeight >= thisHeightStart && thisHeight <= nextHeightStart) && (steepness >= details[i].minSlope && steepness <= details[i].maxSlope))
                    {
                        detailMap[y, x] = 1; // Similar to the splat map system, the x and y are reversed.
                    }
                    
                }
            }
            // Again similar to the splat map system. 
            terrainData.SetDetailLayer(0, 0, i, detailMap);
        }
        
    }
    public void AddNewDetails()
    {
        details.Add(new Detail());
    }
    public void RemoveDetails()
    {
        List<Detail> keptDetails = new List<Detail>();
        for (int i = 0; i < details.Count; i++)
        {
            if(!details[i].remove)
            {
                keptDetails.Add(details[i]);
            }
        }
        if(keptDetails.Count == 0)
        {
            keptDetails.Add(details[0]);
        }
        details = keptDetails;
    }

    public void PlantVegetation ()
    {
        // In theory you could use the TreePrototype for rocks, buildings and so on.
        TreePrototype[] newTreePrototypes;
        newTreePrototypes = new TreePrototype[vegetation.Count];
        int tindex = 0;
        foreach (Vegetation t in vegetation)
        {
            newTreePrototypes[tindex] = new TreePrototype();
            newTreePrototypes[tindex].prefab = t.Mesh;
            tindex++;
        }
        terrainData.treePrototypes = newTreePrototypes;

        List<TreeInstance> allVegetation = new List<TreeInstance>();
        for (int z = 0; z < terrainData.size.z; z += treeSpacing)
        {
            for (int x = 0; x < terrainData.size.x; x += treeSpacing)
            {
                for (int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
                {
                    // The higher the density, the higher the chance that the random value is going to be bigger that it, therefore we can plant the tree. Otherwise it breaks. 

                    if (UnityEngine.Random.Range(0.0f, 1.0f) > vegetation[tp].density) break;
                    float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
                    float thisHeightStart = vegetation[tp].minHeight;
                    float thisHeightEnd = vegetation[tp].maxHeight;
                    float steepness = terrainData.GetSteepness(x / (float)terrainData.size.x, z / (float)terrainData.size.z); // Remember to normalize the values for x and z        
                    if ((thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd) && (steepness >= vegetation[tp].minSlope && steepness <= vegetation[tp].maxSlope))
                    {
                        TreeInstance instance = new TreeInstance();
                        instance.position = new Vector3((x + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.x, 
                                                            terrainData.GetHeight(x, z)/ terrainData.size.y, 
                                                            (z + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.z);

                        Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x, 
                            instance.position.y * terrainData.size.y,
                            instance.position.z * terrainData.size.z) + this.transform.position; 
                        RaycastHit hit;
                        int layerMask = 1 << terrainLayer;
                        if(Physics.Raycast(treeWorldPos + new Vector3(0,10,0), -Vector3.up, out hit, 100, layerMask) || 
                            Physics.Raycast(treeWorldPos - new Vector3(0,10,0), Vector3.up, out hit, 100, layerMask)) 
                        {
                            float treeHeight = (hit.point.y - this.transform.position.y) / terrainData.size.y;
                            instance.position = new Vector3(instance.position.x * terrainData.size.x / terrainData.alphamapWidth,
                                instance.position.y,
                                instance.position.z * terrainData.size.z / terrainData.alphamapHeight);
                            instance.rotation = UnityEngine.Random.Range(0, 360);
                            instance.prototypeIndex = tp;
                            instance.color = Color.Lerp(vegetation[tp].color1, vegetation[tp].color2, UnityEngine.Random.Range(0.0f, 1.0f));
                            instance.lightmapColor = vegetation[tp].lightColor;
                            instance.heightScale = UnityEngine.Random.Range(vegetation[tp].minScale, vegetation[tp].maxScale);
                            instance.widthScale = UnityEngine.Random.Range(vegetation[tp].minScale, vegetation[tp].maxScale);

                            allVegetation.Add(instance);
                            if (allVegetation.Count >= maxTrees) goto TREESDONE;
                        }
                        
                    }
                    
                }
            }
            
        }
    TREESDONE:
        terrainData.treeInstances = allVegetation.ToArray();
    }

    public void AddNewVegetation ()
    {
        vegetation.Add(new Vegetation());
    }
    public void RemoveVegetation()
    {
        List<Vegetation> keptVegetation = new List<Vegetation>();
        for (int i = 0; i < vegetation.Count; i++)
        {
            if (!vegetation[i].remove)
            {
                keptVegetation.Add(vegetation[i]);
            }
        }
        if (keptVegetation.Count == 0) // don't want to keep any
        {
            keptVegetation.Add(vegetation[0]); // add at least 1
        }
        vegetation = keptVegetation;
    }

    float [,] GetHeightMap()
    {
        if (!resetTerrain)
        {
            return terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        }else
        {
            return new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        }
    }

    public void Perlin()
    {
        float[,] heightMap = GetHeightMap();
        for (int y = 0; y< terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                // Remember that pelinHeightScale is an extra scaling factor on top of the heightmap resolution. 
                heightMap[x, y] += Utilis.fBM((x + perlinOffsetX) * perlinXScale, (y+ perlinOffsetY) * perlinYScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void MultiplePerlin()
    {
        float[,] heightMap = GetHeightMap();
        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                foreach (PerlinParameters p in perlinParameters)
                {
                    heightMap[x, y] += Utilis.fBM((x + p.mPerlinOffsetX) * p.mPerlinXScale, (y + p.mPerlinOffsetY) * p.mPerlinYScale, p.mPerlinOctaves, p.mPerlinPersistance) * p.mPerlinHeightScale;
                }
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    
    public void AddPerlin()
    {
        perlinParameters.Add(new PerlinParameters());
    }
    
    public void RemovePerlin()
    {
        List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();
        for (int i = 0; i < perlinParameters.Count; i++)
        {
            if(!perlinParameters[i].remove)
            {
                keptPerlinParameters.Add(perlinParameters[i]);
            }
            
        }
        if(keptPerlinParameters.Count == 0) // don't want to keep any 
        {
            keptPerlinParameters.Add(perlinParameters[0]); // add at least one othewise we get an error.
        }
        perlinParameters = keptPerlinParameters;
    }

    
    public void SplatMaps()
    {
        TerrainLayer[] newSplatPrototypes;
        newSplatPrototypes = new TerrainLayer[splatHeights.Count]; 
        int spindex = 0;
        foreach (SplatHeights sh in splatHeights) 
        {
            newSplatPrototypes[spindex] = new TerrainLayer();
            newSplatPrototypes[spindex].diffuseTexture = sh.texture;
            newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
            newSplatPrototypes[spindex].tileSize = sh.tileSize;
            newSplatPrototypes[spindex].diffuseTexture.Apply(true);
            spindex++;
        }
        terrainData.terrainLayers = newSplatPrototypes; // Finally we tell the terrain where to find these prototypes. 

        float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        float[,,] splatMapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float[] splat = new float[terrainData.alphamapLayers]; // For example in splat [0] it will hold the blend value for the first texture. 
                for (int i = 0; i < splatHeights.Count; i++)
                {
                    // To make the blending seem more realistic and not follow a straight line we add some perlin noise to the blending.
                    float noise = Mathf.PerlinNoise(x * splatNoiseXScale, y * splatNoiseYScale) * splatNoiseScaler;
                    float offset = splatOffset + noise;
                    float thisHeightStart = splatHeights[i].minHeight -offset;
                    float thisHeightStop = splatHeights[i].maxHeight + offset;
                    // float steepness = GetSteepness(heightMap, x, y, terrainData.heightmapResolution, terrainData.heightmapResolution);
                    float steepness = terrainData.GetSteepness(y / (float)terrainData.alphamapHeight, x / (float)terrainData.alphamapWidth);
                    // In addition to testing whether a texture can be placed on a particular height value, we also test whether it can be placed on a certain steepness.
                    if ((heightMap[x, y] >= thisHeightStart && heightMap[x, y] <= thisHeightStop) && (steepness >= splatHeights[i].minSlope && steepness <= splatHeights[i].maxSlope)) 
                    {
                        // If at a particular point in the heightmap, the particular texture is indeed inside the min and max height, the splat array will have a 1. Otherwise it will be 0.
                        splat[i] = 1; // Remember this array is just a column, or better said a vector for each point in the heightmap.
                    }
                }
                NormalizeVector(splat);
                for (int j = 0; j < splatHeights.Count; j++) 
                {
                    splatMapData[x, y, j] = splat[j]; 
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, splatMapData);
    }

    void NormalizeVector(float[] v)
    {
        float total = 0;
        for (int i = 0; i < v.Length; i++)
        {
            total += v[i];
        }
        for (int i = 0; i < v.Length; i++)
        {
            v[i] /= total;
        }
    }

    // Function to determine the steepness of the pixels in the heightmap. Can be used to set types of texture on certain values. 
    float GetSteepness(float[,] heightMap, int x, int y, int width, int height)
    {
        float h = heightMap[x, y];
        int nx = x + 1;
        int ny = y + 1;
        if (nx > width - 1)
        {
            nx = x - 1;
        }
        if (ny > height - 1)
        {
            ny = y - 1;
        }
        float dx = heightMap[nx, y] - h;
        float dy = heightMap[x, ny] - h;
        Vector2 gradient = new Vector2(dx, dy);

        float steep = gradient.magnitude;

        return steep;


    }

    public void AddNewSplatHeight()
    {
        splatHeights.Add(new SplatHeights());
    }

    public void RemoveSplatHeight ()
    {
        List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
        for (int i = 0; i < splatHeights.Count; i++)
        {
            if (!splatHeights[i].remove)
            {
                keptSplatHeights.Add(splatHeights[i]);
            }
        }
        if(keptSplatHeights.Count == 0) // don't want to keep any
        {
            keptSplatHeights.Add(splatHeights[0]); // add at least 1
        }
        splatHeights = keptSplatHeights;
    }

    public void Voronoi()
    {
        float[,] heightMap = GetHeightMap();

        // To add multiple random picks we nest everything in another for loop that will run for the amount of peaks we want to have. 
        for (int p = 0; p < voronoiPeaks; p++)
        {
           
            Vector3 peak = new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapResolution),
                                        
                                        UnityEngine.Random.Range(voronoiMinHeight, voronoiMaxHeight),
                                        UnityEngine.Random.Range(0, terrainData.heightmapResolution));
            if(heightMap[(int)peak.x, (int)peak.z] < peak.y)
            {
                heightMap[(int)peak.x, (int)peak.z] = peak.y;
            }else
            {
                continue;
            }
            Vector2 peakLocation = new Vector2(peak.x, peak.z);
            float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(terrainData.heightmapResolution, terrainData.heightmapResolution));

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {

                    if (!(x == peak.x && y == peak.z)) // if we do not find ourselves at the peak
                    {
                        float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, y)) / maxDistance;
                        float h;
                        if (voronoiType == VoronoiType.Combined)
                        {
                            h = peak.y - distanceToPeak * voronoiFallOff - Mathf.Pow(distanceToPeak, voronoiDropOff); // combined
                        } else if (voronoiType == VoronoiType.Power)
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak, voronoiDropOff) * voronoiFallOff; // power
                        } else if (voronoiType == VoronoiType.Mushroom) 
                        {
                            h = peak.y - Mathf.Pow(distanceToPeak * 3, voronoiFallOff) - Mathf.Sin(distanceToPeak * 2 * Mathf.PI) / voronoiDropOff; // sin power looks like a mushroom almost
                        } else
                        {
                            h = peak.y - distanceToPeak * voronoiFallOff; // linear
                        }
                        

                        // Important check for multiple peaks. We have to check if at each point our height is less than the one we're trying to set.
                        // We have to check if there is already a height at each point and change it only if the new height is bigger than the one before. 
                        if (heightMap [x, y] < h)
                        {
                            heightMap[x, y] = h;
                        }
                        
                    }
                }
            }
        }
        
        terrainData.SetHeights(0, 0, heightMap);
    } 

    public void MidPointDisplacement()
    {
        float[,] heightMap = GetHeightMap();
        int width = terrainData.heightmapResolution - 1;
        int squareSize = width;
        float heightMin = MPDheightMin;
        float heightMax = MPDheightMax;
        float heightDampener = (float)Mathf.Pow(MPDheightDampenerPower, -1 * MPDroughness);

        int cornerX, cornerY;
        int midX, midY;
        int pmidXL, pmidXR, pmidYU, pmidYD;

        while (squareSize > 0) 
        {
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    heightMap[midX, midY] = (float)((heightMap[x, y] + heightMap[cornerX, y] + heightMap[x, cornerY] + heightMap[cornerX, cornerY]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));

                }
            }
            
            for (int x = 0; x < width; x += squareSize)
            {
                for (int y = 0; y < width; y += squareSize)
                {
                    cornerX = (x + squareSize);
                    cornerY = (y + squareSize);

                    midX = (int)(x + squareSize / 2.0f);
                    midY = (int)(y + squareSize / 2.0f);

                    pmidXR = (int)(midX + squareSize);
                    pmidYU = (int)(midY + squareSize);
                    pmidXL = (int)(midX - squareSize);
                    pmidYD = (int)(midY - squareSize);

                    if (pmidXL <= 0 || pmidYD <= 0 || pmidXR >= width -1 || pmidYU >= width - 1)
                    {
                        continue;
                    } 
                    heightMap[midX, y] = (float)((heightMap[x, y] + heightMap[midX, midY] + heightMap[cornerX, y] + heightMap[midX, pmidYD]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                    heightMap[cornerX, midY] = (float)((heightMap[midX, midY] + heightMap[cornerX, y] + heightMap[pmidXR, midY] + heightMap[cornerX, cornerY]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                    heightMap[midX, cornerY] = (float)((heightMap[midX, midY] + heightMap[cornerX, cornerY] + heightMap[midX, pmidYU] + heightMap[x, cornerY]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                    heightMap[x, midY] = (float)((heightMap[pmidXL, midY] + heightMap[x, y] + heightMap[midX, midY] + heightMap[x, cornerY]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
                }
            }

            squareSize = (int)(squareSize / 2.0f);
            heightMin *= heightDampener;
            heightMax *= heightDampener;
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    // Helper method to find the neighbouring pixels in a heightmap
    List <Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
    {
        List<Vector2> neighbours = new List<Vector2>();
        for (int y = -1; y < 2; y++) 
        {
            for (int x = -1; x < 2; x++) 
            {
                if (!(x == 0 && y == 0)) 
                {
                    Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1), Mathf.Clamp(pos.y + y, 0, height - 1));  
                    if(!neighbours.Contains(nPos))
                    {
                        neighbours.Add(nPos);
                    }
                }
            }
        }
        return neighbours;
    }

    public void Smooth()
    {
        float[,] heightMap = GetHeightMap();
        float smoothProgress = 0;
        EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress);
        for (int s = 0; s < smoothAmount; s++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    float avgHeight = heightMap[x, y];
                    List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, y), terrainData.heightmapResolution, terrainData.heightmapResolution);

                    foreach (Vector2 n in neighbours)
                    {
                        avgHeight += heightMap[(int)n.x, (int)n.y];
                    }
                    heightMap[x, y] = avgHeight / ((float)neighbours.Count + 1);

                }
            }
            smoothProgress++;
            EditorUtility.DisplayProgressBar("Smoothing Terrain", "Progress", smoothProgress / smoothAmount);
        }
        
        terrainData.SetHeights(0, 0, heightMap);
        EditorUtility.ClearProgressBar();
    }
    
    public void RandomTerrain()
    {
        float[,] heightMap = GetHeightMap();
      
        
        for (int x = 0 ; x < terrainData.heightmapResolution; x++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                heightMap[x, y] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void LoadTexture()
    {
        float[,] heightMap;
        heightMap = GetHeightMap();
        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < terrainData.heightmapResolution; z++)
            { 
                heightMap[x, z] += heightMapImage.GetPixel((int)(x * heightMapScale.x), (int)(z * heightMapScale.z)).grayscale * heightMapScale.y;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    public void ResetTerrainHeights()
    {
        float [,] heightMap;
        heightMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
        for (int x = 0; x < terrainData.heightmapResolution; x++)
        {
            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                heightMap[x, y] = 0f;
            }
        }
        terrainData.SetHeights(0, 0, heightMap);
    }

    void OnEnable()
    {
        Debug.Log("Initialising Terrain Data");
        terrain = this.GetComponent<Terrain>();
        terrainData = Terrain.activeTerrain.terrainData;
    }

    
    public enum TagType { Tag = 0, Layer = 1}
    [SerializeField]
    int terrainLayer = -1;

    void Awake()
    {

        
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        AddTag(tagsProp, "Terrain", TagType.Tag);
        AddTag(tagsProp, "Cloud", TagType.Tag);
        AddTag(tagsProp, "Shore", TagType.Tag);

        tagManager.ApplyModifiedProperties();

        SerializedProperty layerProp = tagManager.FindProperty("layers");
        AddTag(layerProp, "Sky", TagType.Layer);
        terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
        tagManager.ApplyModifiedProperties();

        this.gameObject.tag = "Terrain";
        this.gameObject.layer = terrainLayer;

    }

    
    int AddTag (SerializedProperty tagsProp, string newTag , TagType tType)
    {
        bool found = false;
        // ensure the tag does not already exist
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if(t.stringValue.Equals(newTag))
            {
                found = true;
                return i;
            }
        }
        // add new tag
        if(!found && tType == TagType.Tag)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
            newTagProp.stringValue = newTag;
        }
        // add new layer
        else if (!found && tType == TagType.Layer)
        {
            // We start with j = 8 because all other layers before 8 are already taken by UNITY.
            for (int j = 8; j < tagsProp.arraySize; j++)
            {
                SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);
                // add layer in next empty slot
                if (newLayer.stringValue == "")
                {
                    Debug.Log("Adding New Layer: " + newTag);
                    newLayer.stringValue = newTag;
                    return j;
                }
            }
        }
        return -1;
    }
   
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
