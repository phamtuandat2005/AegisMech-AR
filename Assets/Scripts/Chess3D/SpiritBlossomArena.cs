using UnityEngine;
using MechChess.ProceduralMech;

namespace MechChess.Chess3D
{
    /// <summary>
    /// Generates the Spirit Blossom / Ionia celestial floating island sanctuary environment
    /// inspired faithfully by Teamfight Tactics (TFT Spirit Blossom Arena).
    /// Features:
    /// - Authentic 3D Models downloaded from the internet:
    ///   * Photogrammetry-scanned authentic Japanese Sakura Trees (Models/Environment/SakuraTree)
    ///   * Carved ancient Stone Sanctuary Stairs (Models/Environment/StoneStairs)
    ///   * Natural 3D Cliff Crags and Boulders (Models/Environment/Rock01, Rock02)
    /// - High-resolution PolyHaven PBR Textures:
    ///   * Lush Ionian Grass (Grass_Diffuse, Grass_Normal)
    ///   * Ancient Sanctuary Temple Stone Pavers (StoneTiles_Diffuse, StoneTiles_Normal)
    /// - 6 outer satellite pedestals at 60-degree radial positions with connecting stone stairs,
    ///   runic crests, and translucent lilac spirit ward domes.
    /// - Mystical cyan spirit lotus pond with glowing turquoise water, lily pads, and blooming spirit lotuses.
    /// - Continuous particle drift of falling sakura blossom petals.
    /// - Floating animated spirit wisps and gentle celestial cloud sea below.
    /// </summary>
    public static class SpiritBlossomArena
    {
        private static Material _grassMat;
        private static Material _mossMat;
        private static Material _cliffStoneMat;
        private static Material _templeStoneMat;
        private static Material _stoneTrimMat;
        private static Material _runeGlowMat;
        private static Material _goldRuneMat;
        private static Material _barkMat;
        private static Material _sakuraPinkMat;
        private static Material _sakuraSoftPinkMat;
        private static Material _sakuraDeepPinkMat;
        private static Material _spiritWaterMat;
        private static Material _lotusPetalMat;
        private static Material _lotusCoreMat;
        private static Material _wardDomeMat;
        private static Material _cloudMat;
        private static Material _spiritOrbCyanMat;
        private static Material _spiritOrbLilacMat;

        public static GameObject BuildArena(Transform parent, Vector3 center)
        {
            // Clean up any existing arena
            Transform existing = parent.Find("SpiritBlossom_ArenaEnvironment");
            if (existing != null) SafeDestroy(existing.gameObject);

            InitMaterials();

            GameObject root = new GameObject("SpiritBlossom_ArenaEnvironment");
            root.transform.SetParent(parent, false);
            root.transform.position = center;

            // 1. Floating Island Base & Real 3D Rock Spire Cliffs
            BuildFloatingIslandBase(root.transform);

            // 2. Sanctuary Terraces & 6 TFT Satellite Dais with Real Stone Stairs
            BuildSanctuaryTerraces(root.transform);

            // 3. Mystical Cyan Spirit Lotus Pond
            BuildSpiritLotusPond(root.transform);

            // 4. Real 3D Japanese Sakura Trees with Atmospheric Glow
            BuildSakuraTrees(root.transform);

            // 5. Drifting Sakura Blossom Petals (Particle System)
            BuildBlossomPetalParticles(root.transform);

            // 6. Floating Animated Spirit Wisps
            BuildSpiritWisps(root.transform);

            // 7. Celestial Cloud Sea Below
            BuildCloudSea(root.transform);

            // 8. Atmospheric Lighting & Camera Sky
            ConfigureAtmosphere(root.transform);

            return root;
        }

        private static void InitMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard")
                            ?? Shader.Find("Diffuse");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                              ?? Shader.Find("Sprites/Default")
                              ?? Shader.Find("Standard");

            // Grass: High-res PBR Turf with normal mapping
            _grassMat = new Material(litShader) { name = "Spirit_LushGrass" };
            ApplyPBRTexture(_grassMat, "Textures/Environment/Grass_Diffuse", "Textures/Environment/Grass_Normal",
                new Color(0.35f, 0.72f, 0.38f), new Vector2(10f, 10f), 0.0f, 0.20f);

            // Deep Moss Accent
            _mossMat = new Material(litShader) { name = "Spirit_DeepMoss" };
            ApplyPBRTexture(_mossMat, "Textures/Environment/Grass_Diffuse", "Textures/Environment/Grass_Normal",
                new Color(0.20f, 0.48f, 0.24f), new Vector2(14f, 14f), 0.0f, 0.15f);

            // Cliff Rock: Weathered basalt and granite
            _cliffStoneMat = new Material(litShader) { name = "Spirit_CliffStone" };
            ApplyPBRTexture(_cliffStoneMat, "Textures/Environment/StoneTiles_Diffuse", "Textures/Environment/StoneTiles_Normal",
                new Color(0.48f, 0.47f, 0.44f), new Vector2(5f, 5f), 0.05f, 0.25f);

            // Temple Stone: Aged mossy sandstone dais
            _templeStoneMat = new Material(litShader) { name = "Spirit_TempleStone" };
            ApplyPBRTexture(_templeStoneMat, "Textures/Environment/StoneTiles_Diffuse", "Textures/Environment/StoneTiles_Normal",
                new Color(0.88f, 0.84f, 0.76f), new Vector2(6f, 6f), 0.04f, 0.35f);

            // Stone Trim / Balustrade
            _stoneTrimMat = new Material(litShader) { name = "Spirit_StoneTrim" };
            ApplyPBRTexture(_stoneTrimMat, "Textures/Environment/StoneTiles_Diffuse", "Textures/Environment/StoneTiles_Normal",
                new Color(0.68f, 0.65f, 0.58f), new Vector2(8f, 2f), 0.05f, 0.40f);

            // Rune Glow: Glowing spiritual jade runes
            _runeGlowMat = new Material(litShader) { name = "Spirit_JadeRuneGlow" };
            SetMat(_runeGlowMat, new Color(0.25f, 0.90f, 0.65f), 0.1f, 0.65f);
            _runeGlowMat.EnableKeyword("_EMISSION");
            if (_runeGlowMat.HasProperty("_EmissionColor"))
                _runeGlowMat.SetColor("_EmissionColor", new Color(0.3f, 1.6f, 0.85f) * 1.8f);

            // Gold Rune Inlay
            _goldRuneMat = new Material(litShader) { name = "Spirit_GoldRune" };
            SetMat(_goldRuneMat, new Color(0.92f, 0.78f, 0.35f), 0.3f, 0.6f);
            _goldRuneMat.EnableKeyword("_EMISSION");
            if (_goldRuneMat.HasProperty("_EmissionColor"))
                _goldRuneMat.SetColor("_EmissionColor", new Color(1.4f, 1.0f, 0.3f) * 1.2f);

            // Tree Bark: Ancient dark bonsai wood (fallback)
            _barkMat = new Material(litShader) { name = "Spirit_TreeBark" };
            SetMat(_barkMat, new Color(0.20f, 0.13f, 0.09f), 0.0f, 0.15f);

            // Sakura Blossom Vibrant Pink (fallback)
            _sakuraPinkMat = new Material(litShader) { name = "Spirit_SakuraPink" };
            SetMat(_sakuraPinkMat, new Color(1.0f, 0.48f, 0.72f), 0.0f, 0.25f);
            _sakuraPinkMat.EnableKeyword("_EMISSION");
            if (_sakuraPinkMat.HasProperty("_EmissionColor"))
                _sakuraPinkMat.SetColor("_EmissionColor", new Color(1.0f, 0.38f, 0.68f) * 0.35f);

            // Sakura Blossom Soft Pastel Pink
            _sakuraSoftPinkMat = new Material(litShader) { name = "Spirit_SakuraSoftPink" };
            SetMat(_sakuraSoftPinkMat, new Color(1.0f, 0.76f, 0.88f), 0.0f, 0.20f);

            // Sakura Blossom Deep Magenta / Plum
            _sakuraDeepPinkMat = new Material(litShader) { name = "Spirit_SakuraDeepPink" };
            SetMat(_sakuraDeepPinkMat, new Color(0.72f, 0.18f, 0.44f), 0.0f, 0.20f);

            // Spirit Water: Luminous turquoise pond
            _spiritWaterMat = new Material(litShader) { name = "Spirit_Water" };
            SetMat(_spiritWaterMat, new Color(0.10f, 0.85f, 0.90f), 0.15f, 0.96f);
            _spiritWaterMat.EnableKeyword("_EMISSION");
            if (_spiritWaterMat.HasProperty("_EmissionColor"))
                _spiritWaterMat.SetColor("_EmissionColor", new Color(0.08f, 0.70f, 0.80f) * 1.9f);

            // Lotus Petal Material
            _lotusPetalMat = new Material(litShader) { name = "Spirit_LotusPetal" };
            SetMat(_lotusPetalMat, new Color(0.98f, 0.72f, 0.88f), 0.0f, 0.3f);
            _lotusPetalMat.EnableKeyword("_EMISSION");
            if (_lotusPetalMat.HasProperty("_EmissionColor"))
                _lotusPetalMat.SetColor("_EmissionColor", new Color(0.9f, 0.4f, 0.7f) * 0.4f);

            // Lotus Core Material
            _lotusCoreMat = new Material(litShader) { name = "Spirit_LotusCore" };
            SetMat(_lotusCoreMat, new Color(1.0f, 0.85f, 0.25f), 0.1f, 0.5f);
            _lotusCoreMat.EnableKeyword("_EMISSION");
            if (_lotusCoreMat.HasProperty("_EmissionColor"))
                _lotusCoreMat.SetColor("_EmissionColor", new Color(1.5f, 1.2f, 0.3f) * 1.5f);

            // Spirit Ward Dome (Translucent iridescent lilac barrier from Image 2)
            _wardDomeMat = new Material(litShader) { name = "Spirit_WardDome" };
            Color domeCol = new Color(0.75f, 0.65f, 0.95f, 0.45f);
            SetMat(_wardDomeMat, domeCol, 0.2f, 0.85f);
            _wardDomeMat.EnableKeyword("_EMISSION");
            if (_wardDomeMat.HasProperty("_EmissionColor"))
                _wardDomeMat.SetColor("_EmissionColor", new Color(0.6f, 0.4f, 1.2f) * 1.4f);

            // Cloud: Soft celestial cloud
            _cloudMat = new Material(unlitShader) { name = "Spirit_Cloud" };
            if (_cloudMat.HasProperty("_BaseColor")) _cloudMat.SetColor("_BaseColor", new Color(0.93f, 0.97f, 1.0f, 0.85f));
            if (_cloudMat.HasProperty("_Color")) _cloudMat.SetColor("_Color", new Color(0.93f, 0.97f, 1.0f, 0.85f));

            // Spirit Orb Cyan
            _spiritOrbCyanMat = new Material(unlitShader) { name = "Spirit_OrbCyan" };
            Color orbCyan = new Color(0.35f, 0.95f, 1.0f);
            if (_spiritOrbCyanMat.HasProperty("_BaseColor")) _spiritOrbCyanMat.SetColor("_BaseColor", orbCyan);
            if (_spiritOrbCyanMat.HasProperty("_Color")) _spiritOrbCyanMat.SetColor("_Color", orbCyan);

            // Spirit Orb Lilac
            _spiritOrbLilacMat = new Material(unlitShader) { name = "Spirit_OrbLilac" };
            Color orbLilac = new Color(0.85f, 0.55f, 1.0f);
            if (_spiritOrbLilacMat.HasProperty("_BaseColor")) _spiritOrbLilacMat.SetColor("_BaseColor", orbLilac);
            if (_spiritOrbLilacMat.HasProperty("_Color")) _spiritOrbLilacMat.SetColor("_Color", orbLilac);
        }

        private static void ApplyPBRTexture(Material mat, string diffPath, string normPath, Color tint, Vector2 tiling, float metallic, float smoothness)
        {
            Texture2D diff = Resources.Load<Texture2D>(diffPath);
            Texture2D norm = Resources.Load<Texture2D>(normPath);

            if (diff != null)
            {
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", diff);
                    mat.SetTextureScale("_BaseMap", tiling);
                }
                else if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", diff);
                    mat.SetTextureScale("_MainTex", tiling);
                }
            }

            if (norm != null)
            {
                if (mat.HasProperty("_BumpMap"))
                {
                    mat.SetTexture("_BumpMap", norm);
                    mat.SetTextureScale("_BumpMap", tiling);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            SetMat(mat, tint, metallic, smoothness);
        }

        private static void SetMat(Material m, Color c, float met, float sm)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", met);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", sm);
            m.enableInstancing = true;
        }

        // =========================================================================
        // 1. FLOATING ISLAND BASE & REAL 3D ROCK SPIRES
        // =========================================================================
        private static void BuildFloatingIslandBase(Transform root)
        {
            GameObject islandBase = new GameObject("FloatingIsland_Base");
            islandBase.transform.SetParent(root, false);

            // Main inverted cliff rock underneath
            GameObject rockCone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rockCone.name = "Island_RockCore";
            rockCone.transform.SetParent(islandBase.transform, false);
            rockCone.transform.localPosition = new Vector3(0, -2.5f, 0);
            rockCone.transform.localScale = new Vector3(28f, 1.6f, 28f);
            StripCollider(rockCone);
            rockCone.GetComponent<Renderer>().sharedMaterial = _cliffStoneMat;

            // Deep bedrock bottom taper
            GameObject rockBottom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rockBottom.name = "Island_BedrockTaper";
            rockBottom.transform.SetParent(islandBase.transform, false);
            rockBottom.transform.localPosition = new Vector3(0, -5.5f, 0);
            rockBottom.transform.localScale = new Vector3(20f, 7.5f, 20f);
            StripCollider(rockBottom);
            rockBottom.GetComponent<Renderer>().sharedMaterial = _cliffStoneMat;

            // Outcropping natural 3D crag rocks along edges (Models/Environment/Rock01 & Rock02)
            GameObject rock01Prefab = Resources.Load<GameObject>("Models/Environment/Rock01");
            GameObject rock02Prefab = Resources.Load<GameObject>("Models/Environment/Rock02");

            Vector3[] cragPositions = new Vector3[] {
                new Vector3(-13.8f, -1.8f, -3.5f),
                new Vector3(14.2f, -1.9f, 3.8f),
                new Vector3(-5.2f, -2.0f, 13.8f),
                new Vector3(5.8f, -1.9f, -14.2f),
                new Vector3(-11.8f, -2.2f, 11.2f),
                new Vector3(11.2f, -2.1f, -11.8f),
                new Vector3(-14.5f, -2.5f, -8.5f),
                new Vector3(14.0f, -2.6f, 9.5f),
                new Vector3(0.0f, -2.4f, 14.8f),
                new Vector3(0.0f, -2.4f, -14.8f),
                new Vector3(-14.8f, -2.2f, 4.0f),
                new Vector3(14.8f, -2.2f, -4.0f)
            };

            for (int i = 0; i < cragPositions.Length; i++)
            {
                Vector3 pos = cragPositions[i];
                GameObject selectedPrefab = (i % 2 == 0) ? rock01Prefab : rock02Prefab;

                if (selectedPrefab != null)
                {
                    GameObject rockWrapper = new GameObject($"RockCrag_3D_{i}");
                    rockWrapper.transform.SetParent(islandBase.transform, false);
                    rockWrapper.transform.localPosition = pos;
                    rockWrapper.transform.localRotation = Quaternion.Euler(
                        UnityEngine.Random.Range(-15f, 15f),
                        i * 30f + UnityEngine.Random.Range(-10f, 10f),
                        UnityEngine.Random.Range(-15f, 15f)
                    );
                    float rockScale = (i % 2 == 0) ? 3.5f : 2.8f;
                    rockWrapper.transform.localScale = Vector3.one * rockScale;

                    GameObject rockInst = Object.Instantiate(selectedPrefab, rockWrapper.transform);
                    StripCollider(rockInst);
                }
                else
                {
                    // Fallback to primitive cube
                    GameObject crag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crag.name = $"RockCrag_{i}";
                    crag.transform.SetParent(islandBase.transform, false);
                    crag.transform.localPosition = pos;
                    crag.transform.localScale = new Vector3(4.2f, 4.2f, 4.2f);
                    crag.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(-15f, 15f));
                    StripCollider(crag);
                    crag.GetComponent<Renderer>().sharedMaterial = _cliffStoneMat;
                }
            }
        }

        // =========================================================================
        // 2. SANCTUARY TERRACES & 6 TFT SATELLITE DAIS WITH REAL STONE STAIRS
        // =========================================================================
        private static void BuildSanctuaryTerraces(Transform root)
        {
            GameObject terraces = new GameObject("Sanctuary_Terraces");
            terraces.transform.SetParent(root, false);

            // 1. Lush Green Grass Plateau (top surface at y = -0.06m)
            GameObject grassTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grassTop.name = "LushGrass_Plateau";
            grassTop.transform.SetParent(terraces.transform, false);
            grassTop.transform.localPosition = new Vector3(0, -0.30f, 0);
            grassTop.transform.localScale = new Vector3(30f, 0.24f, 30f);
            StripCollider(grassTop);
            grassTop.GetComponent<Renderer>().sharedMaterial = _grassMat;

            // 2. Sanctuary Outer Retaining Stone Rim (top at -0.04m)
            GameObject outerRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRim.name = "Sanctuary_OuterStoneRim";
            outerRim.transform.SetParent(terraces.transform, false);
            outerRim.transform.localPosition = new Vector3(0, -0.22f, 0);
            outerRim.transform.localScale = new Vector3(28.5f, 0.18f, 28.5f);
            StripCollider(outerRim);
            outerRim.GetComponent<Renderer>().sharedMaterial = _stoneTrimMat;

            // 3. Central Sanctuary Stone Dais (top at -0.02m, just beneath the chessboard at y=0)
            GameObject stoneDais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stoneDais.name = "CentralSanctuary_StoneDais";
            stoneDais.transform.SetParent(terraces.transform, false);
            stoneDais.transform.localPosition = new Vector3(0, -0.14f, 0);
            stoneDais.transform.localScale = new Vector3(15.5f, 0.12f, 15.5f);
            StripCollider(stoneDais);
            stoneDais.GetComponent<Renderer>().sharedMaterial = _templeStoneMat;

            // 4. Spiritual Jade Rune Ring (circular framing ring around the board at y = -0.015m)
            GameObject runeRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeRing.name = "Spiritual_RuneRing";
            runeRing.transform.SetParent(terraces.transform, false);
            runeRing.transform.localPosition = new Vector3(0, -0.12f, 0);
            runeRing.transform.localScale = new Vector3(13.5f, 0.105f, 13.5f);
            StripCollider(runeRing);
            runeRing.GetComponent<Renderer>().sharedMaterial = _runeGlowMat;

            // 5. 6 SATELLITE DAIS (Radial 60-degree hexagonal layout from Image 2)
            float[] anglesDeg = new float[] { 0f, 60f, 120f, 180f, 240f, 300f };
            float satelliteRadius = 11.2f;

            GameObject stairsPrefab = Resources.Load<GameObject>("Models/Environment/StoneStairs");

            for (int i = 0; i < anglesDeg.Length; i++)
            {
                float rad = anglesDeg[i] * Mathf.Deg2Rad;
                Vector3 satPos = new Vector3(Mathf.Sin(rad) * satelliteRadius, -0.14f, Mathf.Cos(rad) * satelliteRadius);

                // Satellite Base Platform (top at -0.02m)
                GameObject sat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sat.name = $"SatelliteDais_{i}";
                sat.transform.SetParent(terraces.transform, false);
                sat.transform.localPosition = satPos;
                sat.transform.localScale = new Vector3(4.8f, 0.12f, 4.8f);
                StripCollider(sat);
                sat.GetComponent<Renderer>().sharedMaterial = _templeStoneMat;

                // Mossy Inlay Center
                GameObject mossInlay = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mossInlay.name = "MossCenter";
                mossInlay.transform.SetParent(sat.transform, false);
                mossInlay.transform.localPosition = new Vector3(0, 0.52f, 0);
                mossInlay.transform.localScale = new Vector3(0.80f, 0.08f, 0.80f);
                StripCollider(mossInlay);
                mossInlay.GetComponent<Renderer>().sharedMaterial = _grassMat;

                // Ancient Jade Spiral Rune Crest in center
                GameObject runeCrest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                runeCrest.name = "RuneCrest";
                runeCrest.transform.SetParent(sat.transform, false);
                runeCrest.transform.localPosition = new Vector3(0, 0.58f, 0);
                runeCrest.transform.localScale = new Vector3(0.50f, 0.06f, 0.50f);
                StripCollider(runeCrest);
                runeCrest.GetComponent<Renderer>().sharedMaterial = (i % 2 == 0) ? _runeGlowMat : _goldRuneMat;

                // Raised Balustrade along outer rim of satellite
                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "StoneBalustrade";
                rim.transform.SetParent(sat.transform, false);
                rim.transform.localPosition = new Vector3(0, 0.70f, 0);
                rim.transform.localScale = new Vector3(0.98f, 0.22f, 0.98f);
                StripCollider(rim);
                rim.GetComponent<Renderer>().sharedMaterial = _stoneTrimMat;

                // Cut inner recess for balustrade
                GameObject innerCut = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                innerCut.name = "BalustradeInner";
                innerCut.transform.SetParent(sat.transform, false);
                innerCut.transform.localPosition = new Vector3(0, 0.76f, 0);
                innerCut.transform.localScale = new Vector3(0.86f, 0.25f, 0.86f);
                StripCollider(innerCut);
                innerCut.GetComponent<Renderer>().sharedMaterial = _templeStoneMat;

                // Connecting Real 3D Stone Stairs / Bridge to Central Dais
                Vector3 toCenter = (Vector3.zero - satPos).normalized;
                Vector3 bridgeCenter = satPos + toCenter * 2.3f;

                if (stairsPrefab != null)
                {
                    GameObject stairsWrapper = new GameObject($"StoneStairs_3D_{i}");
                    stairsWrapper.transform.SetParent(terraces.transform, false);
                    stairsWrapper.transform.localPosition = new Vector3(bridgeCenter.x, -0.42f, bridgeCenter.z);
                    stairsWrapper.transform.localRotation = Quaternion.LookRotation(-toCenter, Vector3.up);
                    stairsWrapper.transform.localScale = new Vector3(0.50f, 0.42f, 0.55f);

                    GameObject stairsInst = Object.Instantiate(stairsPrefab, stairsWrapper.transform);
                    StripCollider(stairsInst);
                }
                else
                {
                    // Fallback to primitive bridge
                    GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bridge.name = $"StoneBridge_{i}";
                    bridge.transform.SetParent(terraces.transform, false);
                    bridge.transform.localPosition = new Vector3(bridgeCenter.x, -0.16f, bridgeCenter.z);
                    bridge.transform.localRotation = Quaternion.LookRotation(toCenter, Vector3.up);
                    bridge.transform.localScale = new Vector3(1.8f, 0.20f, 2.8f);
                    StripCollider(bridge);
                    bridge.GetComponent<Renderer>().sharedMaterial = _templeStoneMat;
                }

                // In Image 2: The 2 North satellite platforms (angles 0° and 60°) have glowing lilac spirit ward domes!
                if (anglesDeg[i] == 0f || anglesDeg[i] == 60f)
                {
                    GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    dome.name = "SpiritWardDome";
                    dome.transform.SetParent(sat.transform, false);
                    dome.transform.localPosition = new Vector3(0, 2.2f, 0);
                    dome.transform.localScale = new Vector3(0.65f, 1.8f, 0.65f);
                    StripCollider(dome);
                    dome.GetComponent<Renderer>().sharedMaterial = _wardDomeMat;

                    GameObject domeCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    domeCap.name = "DomeCap";
                    domeCap.transform.SetParent(dome.transform, false);
                    domeCap.transform.localPosition = new Vector3(0, 0.5f, 0);
                    domeCap.transform.localScale = new Vector3(1.0f, 0.6f, 1.0f);
                    StripCollider(domeCap);
                    domeCap.GetComponent<Renderer>().sharedMaterial = _wardDomeMat;
                }
            }
        }

        // =========================================================================
        // 3. MYSTICAL CYAN SPIRIT LOTUS POND (With Blooming Lotuses)
        // =========================================================================
        private static void BuildSpiritLotusPond(Transform root)
        {
            GameObject pond = new GameObject("Mystical_SpiritLotusPond");
            pond.transform.SetParent(root, false);
            // Placed at the southern/front terrace edge in front of White rank
            pond.transform.localPosition = new Vector3(0, -0.15f, -6.8f);

            // Wide Curved Stone Basin
            GameObject basin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basin.name = "PondBasin";
            basin.transform.SetParent(pond.transform, false);
            basin.transform.localScale = new Vector3(6.5f, 0.12f, 4.5f);
            StripCollider(basin);
            basin.GetComponent<Renderer>().sharedMaterial = _templeStoneMat;

            // Raised decorative stone rim
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "PondRim";
            rim.transform.SetParent(pond.transform, false);
            rim.transform.localPosition = new Vector3(0, 0.04f, 0);
            rim.transform.localScale = new Vector3(6.8f, 0.08f, 4.8f);
            StripCollider(rim);
            rim.GetComponent<Renderer>().sharedMaterial = _stoneTrimMat;

            // Glowing cyan spirit water surface
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            water.name = "SpiritWaterSurface";
            water.transform.SetParent(pond.transform, false);
            water.transform.localPosition = new Vector3(0, 0.06f, 0);
            water.transform.localScale = new Vector3(5.8f, 0.04f, 3.8f);
            StripCollider(water);
            water.GetComponent<Renderer>().sharedMaterial = _spiritWaterMat;

            // Inner Ripple Ring
            GameObject ripple = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ripple.name = "WaterRippleRing";
            ripple.transform.SetParent(pond.transform, false);
            ripple.transform.localPosition = new Vector3(0, 0.08f, 0);
            ripple.transform.localScale = new Vector3(3.2f, 0.03f, 2.0f);
            StripCollider(ripple);
            ripple.GetComponent<Renderer>().sharedMaterial = _runeGlowMat;

            // Water Lily Pads
            Vector3[] lilyPos = new Vector3[] {
                new Vector3(-1.6f, 0.11f, 0.5f),
                new Vector3(1.8f, 0.11f, -0.4f),
                new Vector3(0.3f, 0.11f, 1.0f),
                new Vector3(-0.9f, 0.11f, -0.7f),
                new Vector3(1.2f, 0.11f, 0.8f)
            };
            foreach (var lp in lilyPos)
            {
                GameObject lily = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lily.name = "LilyPad";
                lily.transform.SetParent(pond.transform, false);
                lily.transform.localPosition = lp;
                lily.transform.localScale = new Vector3(0.65f, 0.02f, 0.65f);
                StripCollider(lily);
                lily.GetComponent<Renderer>().sharedMaterial = _grassMat;
            }

            // 2 Blooming Spirit Lotus Flowers
            CreateSpiritLotus(pond.transform, new Vector3(-1.1f, 0.15f, 0.4f), 0.85f);
            CreateSpiritLotus(pond.transform, new Vector3(1.4f, 0.15f, -0.2f), 0.75f);

            // Pond luminous glow light
            GameObject pLightObj = new GameObject("PondGlowLight");
            pLightObj.transform.SetParent(pond.transform, false);
            pLightObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            Light pLight = pLightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(0.2f, 0.95f, 1.0f);
            pLight.intensity = 2.2f;
            pLight.range = 8.0f;
            pLight.shadows = LightShadows.None;
        }

        private static void CreateSpiritLotus(Transform parent, Vector3 localPos, float scale)
        {
            GameObject lotus = new GameObject("SpiritLotusFlower");
            lotus.transform.SetParent(parent, false);
            lotus.transform.localPosition = localPos;
            lotus.transform.localScale = Vector3.one * scale;

            // Glowing golden-amber core
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "LotusCore";
            core.transform.SetParent(lotus.transform, false);
            core.transform.localPosition = new Vector3(0, 0.08f, 0);
            core.transform.localScale = new Vector3(0.25f, 0.20f, 0.25f);
            StripCollider(core);
            core.GetComponent<Renderer>().sharedMaterial = _lotusCoreMat;

            // 8 Lotus Petals radiating outward and angled up
            for (int p = 0; p < 8; p++)
            {
                float angle = p * 45f;
                GameObject petal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                petal.name = $"Petal_{p}";
                petal.transform.SetParent(lotus.transform, false);
                petal.transform.localRotation = Quaternion.Euler(25f, angle, 0);
                petal.transform.localPosition = petal.transform.localRotation * new Vector3(0, 0.06f, 0.20f);
                petal.transform.localScale = new Vector3(0.15f, 0.035f, 0.28f);
                StripCollider(petal);
                petal.GetComponent<Renderer>().sharedMaterial = _lotusPetalMat;
            }
        }

        // =========================================================================
        // 4. REAL 3D JAPANESE SAKURA CHERRY BLOSSOM TREES
        // =========================================================================
        private static void BuildSakuraTrees(Transform root)
        {
            GameObject treeContainer = new GameObject("SakuraCherryBlossom_Trees");
            treeContainer.transform.SetParent(root, false);

            GameObject sakuraPrefab = Resources.Load<GameObject>("Models/Environment/SakuraTree");

            if (sakuraPrefab != null)
            {
                // Authentic photogrammetry-scanned Japanese Sakura Trees framing the sanctuary
                // 4 corner trees + 2 outer flanking trees
                (Vector3 pos, float scale, float rotY)[] treeConfigs = new (Vector3, float, float)[] {
                    (new Vector3(-12.8f, 1.8f, 6.2f),  9.2f,  45f),   // Northwest
                    (new Vector3(12.8f, 1.8f, 6.2f),   8.8f,  -45f),  // Northeast
                    (new Vector3(-12.8f, 1.8f, -5.8f), 9.0f,  135f),  // Southwest
                    (new Vector3(12.8f, 1.8f, -5.8f),  9.4f,  -130f), // Southeast
                    (new Vector3(-15.8f, 1.6f, 0.2f), 10.2f,  25f),   // West flank
                    (new Vector3(15.8f, 1.6f, 0.2f),  10.2f, -35f)    // East flank
                };

                for (int t = 0; t < treeConfigs.Length; t++)
                {
                    var cfg = treeConfigs[t];
                    GameObject treeWrapper = new GameObject($"SakuraTree_3D_{t}");
                    treeWrapper.transform.SetParent(treeContainer.transform, false);
                    treeWrapper.transform.localPosition = cfg.pos;
                    treeWrapper.transform.localRotation = Quaternion.Euler(0, cfg.rotY, 0);
                    treeWrapper.transform.localScale = Vector3.one * cfg.scale;

                    GameObject treeInst = Object.Instantiate(sakuraPrefab, treeWrapper.transform);
                    StripCollider(treeInst);

                    // Soft pink atmospheric point glow under canopy
                    GameObject glowObj = new GameObject("SakuraAmbientGlow");
                    glowObj.transform.SetParent(treeWrapper.transform, false);
                    glowObj.transform.localPosition = new Vector3(0, 0.35f, 0);
                    Light tLight = glowObj.AddComponent<Light>();
                    tLight.type = LightType.Point;
                    tLight.color = new Color(1.0f, 0.65f, 0.85f);
                    tLight.intensity = 2.2f;
                    tLight.range = 10.0f;
                    tLight.shadows = LightShadows.None;
                }
            }
            else
            {
                // Procedural fallback if model is missing
                CreateSprawlingSakuraTree(treeContainer.transform, new Vector3(-12.5f, -0.3f, -5.0f), 1.4f, 45f);
                CreateSprawlingSakuraTree(treeContainer.transform, new Vector3(12.8f, -0.3f, -4.5f), 1.35f, -50f);
                CreateSprawlingSakuraTree(treeContainer.transform, new Vector3(-12.0f, -0.3f, 6.5f), 1.45f, 125f);
                CreateSprawlingSakuraTree(treeContainer.transform, new Vector3(12.2f, -0.3f, 6.8f), 1.40f, -120f);
            }
        }

        private static void CreateSprawlingSakuraTree(Transform parent, Vector3 localPos, float scale, float rotationY)
        {
            GameObject tree = new GameObject("SakuraTree_Procedural");
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = localPos;
            tree.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
            tree.transform.localScale = Vector3.one * scale;

            GameObject trunkBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunkBase.name = "TrunkBase";
            trunkBase.transform.SetParent(tree.transform, false);
            trunkBase.transform.localPosition = new Vector3(0, 1.0f, 0);
            trunkBase.transform.localScale = new Vector3(0.9f, 1.0f, 0.9f);
            trunkBase.transform.localRotation = Quaternion.Euler(15f, 0, -18f);
            StripCollider(trunkBase);
            trunkBase.GetComponent<Renderer>().sharedMaterial = _barkMat;

            GameObject midTier = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            midTier.name = "BlossomCloud";
            midTier.transform.SetParent(tree.transform, false);
            midTier.transform.localPosition = new Vector3(0, 3.6f, 0);
            midTier.transform.localScale = new Vector3(5.0f, 1.2f, 4.5f);
            StripCollider(midTier);
            midTier.GetComponent<Renderer>().sharedMaterial = _sakuraPinkMat;
        }

        // =========================================================================
        // 5. DRIFTING SAKURA BLOSSOM PETALS (Particle System)
        // =========================================================================
        private static void BuildBlossomPetalParticles(Transform root)
        {
            GameObject pObj = new GameObject("SakuraPetalDrift_ParticleSystem");
            pObj.transform.SetParent(root, false);
            pObj.transform.localPosition = new Vector3(0, 7.8f, 0);

            ParticleSystem ps = pObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.duration = 5.0f;
            main.startLifetime = 8.5f;
            main.startSpeed = 0.95f;
            main.startSize = 0.17f;
            main.startColor = new Color(1.0f, 0.65f, 0.82f, 0.95f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 220;

            var emission = ps.emission;
            emission.rateOverTime = 26;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 1.8f, 24f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);   // gentle breeze to right
            vel.y = new ParticleSystem.MinMaxCurve(-0.75f, -1.25f); // floating downward
            vel.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.35f);

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-50f, 50f);

            ParticleSystemRenderer rend = pObj.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            Material petalMat = GetPetalMaterial();
            if (petalMat != null) rend.material = petalMat;

            ps.Play();
        }

        private static Material GetPetalMaterial()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Particles/Standard Unlit")
                    ?? Shader.Find("Sprites/Default");
            Material m = new Material(s);
            Color c = new Color(1.0f, 0.58f, 0.78f, 0.95f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        // =========================================================================
        // 6. FLOATING ANIMATED SPIRIT WISPS (Little Legends)
        // =========================================================================
        private static void BuildSpiritWisps(Transform root)
        {
            GameObject orbContainer = new GameObject("SpiritWisps_Lanterns");
            orbContainer.transform.SetParent(root, false);

            Vector3[] wispPositions = new Vector3[] {
                new Vector3(-5.2f, 1.4f, -4.8f),
                new Vector3(5.5f, 1.6f, -4.5f),
                new Vector3(-4.2f, 1.3f, 5.2f),
                new Vector3(4.6f, 1.5f, 4.9f),
                new Vector3(-8.5f, 1.8f, 0f),
                new Vector3(8.8f, 1.7f, 0.5f)
            };

            for (int i = 0; i < wispPositions.Length; i++)
            {
                bool isCyan = (i % 2 == 0);
                GameObject wisp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                wisp.name = $"SpiritWisp_{(isCyan ? "Cyan" : "Lilac")}_{i}";
                wisp.transform.SetParent(orbContainer.transform, false);
                wisp.transform.localPosition = wispPositions[i];
                wisp.transform.localScale = Vector3.one * 0.32f;
                StripCollider(wisp);
                wisp.GetComponent<Renderer>().sharedMaterial = isCyan ? _spiritOrbCyanMat : _spiritOrbLilacMat;

                // Add Bobbing Animation component
                var bobber = wisp.AddComponent<SpiritWispBobber>();
                bobber.initialPos = wispPositions[i];
                bobber.frequency = 1.8f + i * 0.25f;
                bobber.amplitude = 0.22f;

                // Gentle luminous point light
                Light l = wisp.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = isCyan ? new Color(0.3f, 0.95f, 1.0f) : new Color(0.85f, 0.55f, 1.0f);
                l.intensity = 1.6f;
                l.range = 4.5f;
                l.shadows = LightShadows.None;
            }
        }

        // =========================================================================
        // 7. CELESTIAL CLOUD SEA BELOW
        // =========================================================================
        private static void BuildCloudSea(Transform root)
        {
            GameObject clouds = new GameObject("CelestialCloudSea_Below");
            clouds.transform.SetParent(root, false);

            Vector3[] cloudOffsets = new Vector3[] {
                new Vector3(-15f, -7.0f, -11f),
                new Vector3(14f, -7.2f, -9f),
                new Vector3(-12f, -7.1f, 13f),
                new Vector3(13f, -6.8f, 12f),
                new Vector3(0f, -8.5f, -18f),
                new Vector3(0f, -8.6f, 18f),
                new Vector3(-20f, -9.0f, 0f),
                new Vector3(20f, -9.0f, 0f)
            };
            Vector3[] cloudScales = new Vector3[] {
                new Vector3(20f, 5.5f, 16f),
                new Vector3(18f, 5.2f, 17f),
                new Vector3(19f, 5.6f, 18f),
                new Vector3(17f, 5.0f, 15f),
                new Vector3(22f, 6.0f, 18f),
                new Vector3(22f, 6.0f, 18f),
                new Vector3(24f, 6.5f, 20f),
                new Vector3(24f, 6.5f, 20f)
            };

            for (int i = 0; i < cloudOffsets.Length; i++)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                c.name = $"CloudBank_{i}";
                c.transform.SetParent(clouds.transform, false);
                c.transform.localPosition = cloudOffsets[i];
                c.transform.localScale = cloudScales[i];
                StripCollider(c);
                c.GetComponent<Renderer>().sharedMaterial = _cloudMat;
            }
        }

        // =========================================================================
        // 8. ATMOSPHERIC LIGHTING & CAMERA SKY
        // =========================================================================
        private static void ConfigureAtmosphere(Transform root)
        {
            // Set Camera Background to Serene Ionian Teal-Cyan Sky
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.18f, 0.58f, 0.65f); // Serene Ionian Sky
            }

            // Directional Golden Sun Key Light
            Transform oldLight = root.parent != null ? root.parent.Find("ArenaKeyLight") : null;
            if (oldLight != null) SafeDestroy(oldLight.gameObject);

            GameObject sunObj = new GameObject("SpiritBlossom_GoldenSunLight");
            sunObj.transform.SetParent(root, false);
            Light sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1.0f, 0.97f, 0.90f); // Warm natural golden sun
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.shadowBias = 0.05f;
            sun.shadowNormalBias = 0.4f;
            sunObj.transform.localRotation = Quaternion.Euler(52f, -35f, 0);

            // Overhead Sky Fill Light
            GameObject skyFillObj = new GameObject("SpiritBlossom_SkyFillLight");
            skyFillObj.transform.SetParent(root, false);
            skyFillObj.transform.localPosition = new Vector3(0, 9.0f, 0);
            Light fill = skyFillObj.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(0.40f, 0.88f, 0.96f); // Soft cyan sky fill
            fill.intensity = 2.0f;
            fill.range = 35f;
            fill.shadows = LightShadows.None;
        }

        private static void StripCollider(GameObject go)
        {
            if (go == null) return;
            Collider[] cols = go.GetComponentsInChildren<Collider>(true);
            foreach (var col in cols)
            {
                SafeDestroy(col);
            }
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }

    /// <summary>
    /// Animates spirit wisps floating and bobbing gently like in Teamfight Tactics.
    /// </summary>
    public class SpiritWispBobber : MonoBehaviour
    {
        public Vector3 initialPos;
        public float frequency = 2f;
        public float amplitude = 0.25f;

        private void Update()
        {
            float yOffset = Mathf.Sin(Time.time * frequency) * amplitude;
            transform.localPosition = initialPos + new Vector3(0, yOffset, 0);
        }
    }
}
