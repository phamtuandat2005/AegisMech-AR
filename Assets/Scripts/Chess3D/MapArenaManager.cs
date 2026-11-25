using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MechChess.Chess3D
{
    /// <summary>
    /// MapArenaManager: Dynamic 3D Arena Environment System for AegisMech-AR.
    /// Builds 4 fully realized 3D battle arenas matching the Shop themes:
    /// 1. Spirit Blossom Sanctuary (Thánh Địa Hoa Anh Đào - Japanese Sakura & Lotus)
    /// 2. Classical Pantheon (Đấu Trường Hy Lạp Cổ - Greek Marble Amphitheater & Braziers)
    /// 3. Magma Hellfire Core (Địa Ngục Dung Nham - Volcanic Basalt, Lava Rivers & Embers)
    /// 4. Glacial Arcane Matrix (Cực Quang Pha Lê Băng - Frost Ice, Sapphire Crystals & Runes)
    /// </summary>
    public static class MapArenaManager
    {
        private const string RootName = "Arena_Environment_Root";

        public static GameObject BuildArena(Transform parent, Vector3 center, string themeName = null)
        {
            if (parent == null) return null;

            if (string.IsNullOrEmpty(themeName))
            {
                themeName = PlayerPrefs.GetString(MechChess.UI.MainMenu2D.PrefsEquippedBoard, "Royal Antique Board");
            }

            // Clean up any existing arena environment
            Transform existing = parent.Find(RootName);
            if (existing != null) Object.Destroy(existing.gameObject);

            Transform oldSpirit = parent.Find("SpiritBlossom_ArenaEnvironment");
            if (oldSpirit != null) Object.Destroy(oldSpirit.gameObject);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = center;

            if (themeName.Contains("Dung Nham") || themeName.Contains("Magma"))
            {
                BuildMagmaHellfireArena(root.transform);
            }
            else if (themeName.Contains("Pha Lê") || themeName.Contains("Crystal"))
            {
                BuildGlacialCrystalArena(root.transform);
            }
            else if ((themeName.Contains("Classic") || themeName.Contains("Cổ Điển")) && !themeName.Contains("Hoàng Gia"))
            {
                BuildClassicalPantheonArena(root.transform);
            }
            else
            {
                // Default: Spirit Blossom Sanctuary (with grounded trees)
                SpiritBlossomArena.BuildArena(root.transform, Vector3.zero);
            }

            return root;
        }

        private static Shader GetLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
        }

        private static Shader GetUnlitShader()
        {
            return Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        }

        private static void StripCollider(GameObject go)
        {
            Collider[] cols = go.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) Object.Destroy(cols[i]);
        }

        private static Material CreatePBR(Color col, float metallic, float smoothness, Color? emission = null)
        {
            Material m = new Material(GetLitShader());
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            m.enableInstancing = true;

            if (emission.HasValue && emission.Value != Color.black)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }

        // =========================================================================
        // MAP 2: CLASSICAL PANTHEON ARENA (Đấu Trường Hy Lạp Cổ)
        // Carrara marble forum, fluted ionic colonnade, gold ceremonial braziers
        // =========================================================================
        private static void BuildClassicalPantheonArena(Transform root)
        {
            Material marbleMat = CreatePBR(new Color(0.94f, 0.94f, 0.96f), 0.15f, 0.88f);
            Material goldTrimMat = CreatePBR(new Color(0.92f, 0.78f, 0.32f), 0.85f, 0.80f, new Color(0.8f, 0.6f, 0.1f) * 0.4f);
            Material brazierMat = CreatePBR(new Color(0.20f, 0.18f, 0.16f), 0.80f, 0.40f);
            Material flameMat = CreatePBR(new Color(1.0f, 0.55f, 0.10f), 0.0f, 0.2f, new Color(2.5f, 0.8f, 0.15f) * 2.2f);

            // 1. Grand Forum Marble Dais
            GameObject mainDais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mainDais.name = "Pantheon_MarbleDais";
            mainDais.transform.SetParent(root, false);
            mainDais.transform.localPosition = new Vector3(0, -0.30f, 0);
            mainDais.transform.localScale = new Vector3(26.0f, 0.25f, 26.0f);
            StripCollider(mainDais);
            mainDais.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // Outer Gold Border Ring
            GameObject goldRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            goldRing.name = "Pantheon_GoldRing";
            goldRing.transform.SetParent(root, false);
            goldRing.transform.localPosition = new Vector3(0, -0.28f, 0);
            goldRing.transform.localScale = new Vector3(24.5f, 0.27f, 24.5f);
            StripCollider(goldRing);
            goldRing.GetComponent<Renderer>().sharedMaterial = goldTrimMat;

            // Inner Marble Steppes (Sub-platform directly under chessboard)
            GameObject subPlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            subPlatform.name = "Pantheon_SubPlatform";
            subPlatform.transform.SetParent(root, false);
            subPlatform.transform.localPosition = new Vector3(0, -0.15f, 0);
            subPlatform.transform.localScale = new Vector3(14.0f, 0.14f, 14.0f);
            StripCollider(subPlatform);
            subPlatform.GetComponent<Renderer>().sharedMaterial = marbleMat;

            // 2. Colonnade: 8 Classical Fluted Columns around perimeter
            float colRadius = 10.8f;
            int colCount = 8;
            for (int i = 0; i < colCount; i++)
            {
                float angle = i * (Mathf.PI * 2f / colCount);
                Vector3 colPos = new Vector3(Mathf.Cos(angle) * colRadius, 0f, Mathf.Sin(angle) * colRadius);

                GameObject colObj = new GameObject($"Pantheon_Column_{i}");
                colObj.transform.SetParent(root, false);
                colObj.transform.localPosition = colPos;

                // Base pedestal
                GameObject pBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pBase.name = "Base";
                pBase.transform.SetParent(colObj.transform, false);
                pBase.transform.localPosition = new Vector3(0, 0.25f, 0);
                pBase.transform.localScale = new Vector3(1.3f, 0.50f, 1.3f);
                StripCollider(pBase);
                pBase.GetComponent<Renderer>().sharedMaterial = goldTrimMat;

                // Shaft
                GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "Shaft";
                shaft.transform.SetParent(colObj.transform, false);
                shaft.transform.localPosition = new Vector3(0, 2.5f, 0);
                shaft.transform.localScale = new Vector3(0.9f, 2.0f, 0.9f);
                StripCollider(shaft);
                shaft.GetComponent<Renderer>().sharedMaterial = marbleMat;

                // Capital
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cap.name = "Capital";
                cap.transform.SetParent(colObj.transform, false);
                cap.transform.localPosition = new Vector3(0, 4.6f, 0);
                cap.transform.localScale = new Vector3(1.4f, 0.40f, 1.4f);
                StripCollider(cap);
                cap.GetComponent<Renderer>().sharedMaterial = goldTrimMat;
            }

            // 3. 4 Ceremonial Gold Fire Braziers at diagonal corners
            Vector3[] brazierCorners = {
                new Vector3(-6.2f, 0f,  6.2f),
                new Vector3( 6.2f, 0f,  6.2f),
                new Vector3(-6.2f, 0f, -6.2f),
                new Vector3( 6.2f, 0f, -6.2f)
            };

            for (int b = 0; b < brazierCorners.Length; b++)
            {
                GameObject bObj = new GameObject($"Pantheon_Brazier_{b}");
                bObj.transform.SetParent(root, false);
                bObj.transform.localPosition = brazierCorners[b];

                // Stand
                GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stand.name = "Stand";
                stand.transform.SetParent(bObj.transform, false);
                stand.transform.localPosition = new Vector3(0, 0.45f, 0);
                stand.transform.localScale = new Vector3(0.7f, 0.45f, 0.7f);
                StripCollider(stand);
                stand.GetComponent<Renderer>().sharedMaterial = brazierMat;

                // Bowl
                GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bowl.name = "Bowl";
                bowl.transform.SetParent(bObj.transform, false);
                bowl.transform.localPosition = new Vector3(0, 0.95f, 0);
                bowl.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
                StripCollider(bowl);
                bowl.GetComponent<Renderer>().sharedMaterial = goldTrimMat;

                // Glowing Flame Core
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "Flame";
                flame.transform.SetParent(bObj.transform, false);
                flame.transform.localPosition = new Vector3(0, 1.25f, 0);
                flame.transform.localScale = new Vector3(0.55f, 0.75f, 0.55f);
                StripCollider(flame);
                flame.GetComponent<Renderer>().sharedMaterial = flameMat;

                // Warm Dancing Light
                GameObject lightObj = new GameObject("BrazierLight");
                lightObj.transform.SetParent(bObj.transform, false);
                lightObj.transform.localPosition = new Vector3(0, 1.4f, 0);
                Light lt = lightObj.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.color = new Color(1.0f, 0.72f, 0.35f);
                lt.intensity = 2.5f;
                lt.range = 7.5f;
                lt.shadows = LightShadows.None;
            }
        }

        // =========================================================================
        // MAP 3: MAGMA HELLFIRE CORE (Địa Ngục Dung Nham)
        // Scorched basalt rock island, molten lava river, magma obelisks & fire glow
        // =========================================================================
        private static void BuildMagmaHellfireArena(Transform root)
        {
            Material basaltMat = CreatePBR(new Color(0.12f, 0.10f, 0.11f), 0.70f, 0.30f);
            Material lavaMat = CreatePBR(new Color(1.0f, 0.35f, 0.05f), 0.20f, 0.65f, new Color(2.8f, 0.65f, 0.10f) * 2.5f);
            Material obsidianMat = CreatePBR(new Color(0.18f, 0.14f, 0.16f), 0.85f, 0.75f, new Color(0.9f, 0.15f, 0.05f) * 0.8f);

            // 1. Scorched Basalt Bedrock Island
            GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            island.name = "Magma_BasaltIsland";
            island.transform.SetParent(root, false);
            island.transform.localPosition = new Vector3(0, -0.35f, 0);
            island.transform.localScale = new Vector3(28.0f, 0.30f, 28.0f);
            StripCollider(island);
            island.GetComponent<Renderer>().sharedMaterial = basaltMat;

            // Deep Bedrock Underbelly
            GameObject underbelly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            underbelly.name = "Magma_Underbelly";
            underbelly.transform.SetParent(root, false);
            underbelly.transform.localPosition = new Vector3(0, -5.0f, 0);
            underbelly.transform.localScale = new Vector3(22.0f, 8.0f, 22.0f);
            StripCollider(underbelly);
            underbelly.GetComponent<Renderer>().sharedMaterial = basaltMat;

            // 2. Molten Lava Trench encircling the chessboard
            GameObject lavaRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lavaRing.name = "Magma_LavaRing";
            lavaRing.transform.SetParent(root, false);
            lavaRing.transform.localPosition = new Vector3(0, -0.22f, 0);
            lavaRing.transform.localScale = new Vector3(15.5f, 0.14f, 15.5f);
            StripCollider(lavaRing);
            lavaRing.GetComponent<Renderer>().sharedMaterial = lavaMat;

            // Central Basalt Platform for Chessboard
            GameObject centerPlat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerPlat.name = "Magma_CenterPlatform";
            centerPlat.transform.SetParent(root, false);
            centerPlat.transform.localPosition = new Vector3(0, -0.14f, 0);
            centerPlat.transform.localScale = new Vector3(12.5f, 0.12f, 12.5f);
            StripCollider(centerPlat);
            centerPlat.GetComponent<Renderer>().sharedMaterial = obsidianMat;

            // 3. Volcanic Crag Rocks (using Rock01 & Rock02 if available)
            GameObject rock01 = Resources.Load<GameObject>("Models/Environment/Rock01");
            GameObject rock02 = Resources.Load<GameObject>("Models/Environment/Rock02");

            Vector3[] rockPos = {
                new Vector3(-11.5f, -0.2f,  4.5f),
                new Vector3( 11.8f, -0.2f, -5.2f),
                new Vector3(-5.5f,  -0.2f, 11.2f),
                new Vector3( 6.2f,  -0.2f, -11.0f),
                new Vector3(-12.0f, -0.2f, -6.5f),
                new Vector3( 12.2f, -0.2f,  6.8f)
            };

            for (int r = 0; r < rockPos.Length; r++)
            {
                GameObject p = (r % 2 == 0 && rock01 != null) ? rock01 : rock02;
                if (p != null)
                {
                    GameObject rInst = Object.Instantiate(p, root);
                    rInst.name = $"Magma_Rock_{r}";
                    rInst.transform.localPosition = rockPos[r];
                    rInst.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 10f), r * 45f, UnityEngine.Random.Range(-10f, 10f));
                    rInst.transform.localScale = Vector3.one * 3.2f;
                    StripCollider(rInst);
                }
                else
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Magma_BasaltCrag_{r}";
                    cube.transform.SetParent(root, false);
                    cube.transform.localPosition = rockPos[r];
                    cube.transform.localScale = new Vector3(3.2f, 3.8f, 3.2f);
                    cube.transform.localRotation = Quaternion.Euler(15f, r * 50f, 10f);
                    StripCollider(cube);
                    cube.GetComponent<Renderer>().sharedMaterial = basaltMat;
                }
            }

            // 4. 4 Towering Magma Obelisks with Burning Spires
            Vector3[] obeliskCorners = {
                new Vector3(-7.5f, 0f,  7.5f),
                new Vector3( 7.5f, 0f,  7.5f),
                new Vector3(-7.5f, 0f, -7.5f),
                new Vector3( 7.5f, 0f, -7.5f)
            };

            for (int o = 0; o < obeliskCorners.Length; o++)
            {
                GameObject obelisk = new GameObject($"Magma_Obelisk_{o}");
                obelisk.transform.SetParent(root, false);
                obelisk.transform.localPosition = obeliskCorners[o];

                // Pillar
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "Pillar";
                pillar.transform.SetParent(obelisk.transform, false);
                pillar.transform.localPosition = new Vector3(0, 1.8f, 0);
                pillar.transform.localScale = new Vector3(0.95f, 1.8f, 0.95f);
                StripCollider(pillar);
                pillar.GetComponent<Renderer>().sharedMaterial = obsidianMat;

                // Glowing Molten Spire
                GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spire.name = "MoltenCore";
                spire.transform.SetParent(obelisk.transform, false);
                spire.transform.localPosition = new Vector3(0, 3.8f, 0);
                spire.transform.localScale = new Vector3(0.85f, 1.4f, 0.85f);
                StripCollider(spire);
                spire.GetComponent<Renderer>().sharedMaterial = lavaMat;

                // Infernal Point Light
                GameObject oLight = new GameObject("InfernalGlow");
                oLight.transform.SetParent(obelisk.transform, false);
                oLight.transform.localPosition = new Vector3(0, 4.0f, 0);
                Light lt = oLight.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.color = new Color(1.0f, 0.35f, 0.08f);
                lt.intensity = 3.2f;
                lt.range = 9.0f;
                lt.shadows = LightShadows.None;
            }
        }

        // =========================================================================
        // MAP 4: GLACIAL ARCANE MATRIX (Cực Quang Pha Lê Băng)
        // Frost ice floes, rising sapphire crystals & neon cyan rune obelisks
        // =========================================================================
        private static void BuildGlacialCrystalArena(Transform root)
        {
            Material glacialIceMat = CreatePBR(new Color(0.82f, 0.93f, 0.98f), 0.25f, 0.96f, new Color(0.2f, 0.5f, 1.0f) * 0.3f);
            Material sapphireCrystalMat = CreatePBR(new Color(0.15f, 0.45f, 0.95f), 0.40f, 0.98f, new Color(0.3f, 0.8f, 2.5f) * 1.8f);
            Material arcaneRuneMat = CreatePBR(new Color(0.05f, 0.15f, 0.35f), 0.70f, 0.90f, new Color(0.0f, 1.4f, 2.2f) * 1.4f);

            // 1. Frost Glacial Platform
            GameObject icePlat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            icePlat.name = "Glacial_IcePlatform";
            icePlat.transform.SetParent(root, false);
            icePlat.transform.localPosition = new Vector3(0, -0.32f, 0);
            icePlat.transform.localScale = new Vector3(26.0f, 0.28f, 26.0f);
            StripCollider(icePlat);
            icePlat.GetComponent<Renderer>().sharedMaterial = glacialIceMat;

            // Deep Iceberg Core below
            GameObject icebergCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            icebergCore.name = "Glacial_IcebergCore";
            icebergCore.transform.SetParent(root, false);
            icebergCore.transform.localPosition = new Vector3(0, -5.2f, 0);
            icebergCore.transform.localScale = new Vector3(20.0f, 7.5f, 20.0f);
            StripCollider(icebergCore);
            icebergCore.GetComponent<Renderer>().sharedMaterial = glacialIceMat;

            // 2. Arcane Sapphire Inner Dais (Directly beneath chessboard)
            GameObject arcaneDais = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arcaneDais.name = "Glacial_ArcaneDais";
            arcaneDais.transform.SetParent(root, false);
            arcaneDais.transform.localPosition = new Vector3(0, -0.14f, 0);
            arcaneDais.transform.localScale = new Vector3(14.0f, 0.12f, 14.0f);
            StripCollider(arcaneDais);
            arcaneDais.GetComponent<Renderer>().sharedMaterial = arcaneRuneMat;

            // 3. 12 Giant Sapphire Crystal Clusters rising from the ice perimeter
            float crystalRadius = 9.8f;
            int crystalCount = 8;
            for (int c = 0; c < crystalCount; c++)
            {
                float angle = c * (Mathf.PI * 2f / crystalCount);
                Vector3 cPos = new Vector3(Mathf.Cos(angle) * crystalRadius, -0.15f, Mathf.Sin(angle) * crystalRadius);

                GameObject crystalCluster = new GameObject($"Glacial_CrystalCluster_{c}");
                crystalCluster.transform.SetParent(root, false);
                crystalCluster.transform.localPosition = cPos;

                // Main Central Spire
                GameObject mainSpire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mainSpire.name = "MainSpire";
                mainSpire.transform.SetParent(crystalCluster.transform, false);
                mainSpire.transform.localPosition = new Vector3(0, 1.4f, 0);
                mainSpire.transform.localScale = new Vector3(0.55f, 1.4f, 0.55f);
                mainSpire.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-12f, 12f), c * 35f, UnityEngine.Random.Range(-12f, 12f));
                StripCollider(mainSpire);
                mainSpire.GetComponent<Renderer>().sharedMaterial = sapphireCrystalMat;

                // Side Secondary Spire
                GameObject sideSpire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                sideSpire.name = "SideSpire";
                sideSpire.transform.SetParent(crystalCluster.transform, false);
                sideSpire.transform.localPosition = new Vector3(0.45f, 0.9f, 0.2f);
                sideSpire.transform.localScale = new Vector3(0.35f, 0.9f, 0.35f);
                sideSpire.transform.localRotation = Quaternion.Euler(20f, c * 60f, -15f);
                StripCollider(sideSpire);
                sideSpire.GetComponent<Renderer>().sharedMaterial = sapphireCrystalMat;

                // Ethereal Blue Glow
                GameObject cLight = new GameObject("CrystalGlow");
                cLight.transform.SetParent(crystalCluster.transform, false);
                cLight.transform.localPosition = new Vector3(0, 1.8f, 0);
                Light lt = cLight.AddComponent<Light>();
                lt.type = LightType.Point;
                lt.color = new Color(0.2f, 0.75f, 1.0f);
                lt.intensity = 2.0f;
                lt.range = 6.0f;
                lt.shadows = LightShadows.None;
            }

            // 4. Floating Holographic Data Rings
            GameObject ring1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring1.name = "HoloDataRing";
            ring1.transform.SetParent(root, false);
            ring1.transform.localPosition = new Vector3(0, 0.05f, 0);
            ring1.transform.localScale = new Vector3(16.5f, 0.04f, 16.5f);
            StripCollider(ring1);
            ring1.GetComponent<Renderer>().sharedMaterial = sapphireCrystalMat;
        }
    }
}
