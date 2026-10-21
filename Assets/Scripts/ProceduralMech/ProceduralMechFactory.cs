using System;
using System.Collections.Generic;
using UnityEngine;

namespace MechChess.ProceduralMech
{
    public enum PieceType
    {
        Pawn,
        Rook,
        Knight,
        Bishop,
        Queen,
        King
    }

    public enum PieceColor
    {
        White, // Cyber Alliance
        Black  // Dark Syndicate
    }

    /// <summary>
    /// ProceduralMechFactory: Assembles 100% procedural 3D sci-fi mech chess pieces
    /// from basic geometric primitives and URP/Standard emission materials.
    /// No external 3D model assets required.
    /// </summary>
    public static class ProceduralMechFactory
    {
        // Cached Materials
        private static Material _whiteArmorMat;
        private static Material _whiteGlowMat;
        private static Material _blackArmorMat;
        private static Material _blackGlowMat;
        private static Material _frameJointMat;
        private static Material _shieldMat;

        private static Shader _cachedShader;

        /// <summary>
        /// Finds the most suitable shader (URP Lit preferred, falls back to Standard).
        /// </summary>
        private static Shader GetPreferredShader()
        {
            if (_cachedShader != null) return _cachedShader;

            _cachedShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_cachedShader == null) _cachedShader = Shader.Find("URP/Lit");
            if (_cachedShader == null) _cachedShader = Shader.Find("Standard");
            if (_cachedShader == null) _cachedShader = Shader.Find("Mobile/Diffuse");
            if (_cachedShader == null) _cachedShader = Shader.Find("Diffuse");
            return _cachedShader;
        }

        public static void InitializeMaterials()
        {
            Shader shader = GetPreferredShader();

            // 1. White Armor (Cyber Alliance): Silver-Platinum Metallic (Metallic: 0.8, Smoothness: 0.6)
            _whiteArmorMat = new Material(shader) { name = "CyberAlliance_Armor" };
            SetMaterialPBR(_whiteArmorMat, new Color(0.85f, 0.88f, 0.92f), 0.82f, 0.65f);

            // 2. White Glow: Vibrant Cyan Neon (Emission: #00F0FF)
            _whiteGlowMat = new Material(shader) { name = "CyberAlliance_CyanGlow" };
            SetMaterialEmission(_whiteGlowMat, new Color(0.0f, 0.94f, 1.0f), new Color(0.0f, 1.3f, 1.8f) * 2.8f);

            // 3. Black Armor (Dark Syndicate): Dark Carbon Steel (Metallic: 0.9, Smoothness: 0.4)
            _blackArmorMat = new Material(shader) { name = "DarkSyndicate_Armor" };
            SetMaterialPBR(_blackArmorMat, new Color(0.11f, 0.12f, 0.14f), 0.90f, 0.45f);

            // 4. Black Glow: Crimson Neon (Emission: #FF1E40)
            _blackGlowMat = new Material(shader) { name = "DarkSyndicate_CrimsonGlow" };
            SetMaterialEmission(_blackGlowMat, new Color(1.0f, 0.12f, 0.25f), new Color(1.8f, 0.15f, 0.3f) * 2.8f);

            // 5. Joint & Hydraulics: Dark Gunmetal
            _frameJointMat = new Material(shader) { name = "Mech_JointFrame" };
            SetMaterialPBR(_frameJointMat, new Color(0.22f, 0.24f, 0.26f), 0.70f, 0.35f);

            // 6. Energy Shield: Translucent tint
            _shieldMat = new Material(shader) { name = "Mech_EnergyShield" };
            SetMaterialEmission(_shieldMat, new Color(0.0f, 0.8f, 1.0f, 0.6f), new Color(0.0f, 1.0f, 1.5f) * 2.0f);
        }

        private static void SetMaterialPBR(Material mat, Color baseColor, float metallic, float smoothness)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        }

        private static void SetMaterialEmission(Material mat, Color baseColor, Color emissionColor)
        {
            SetMaterialPBR(mat, baseColor, 0.2f, 0.8f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            if (mat.HasProperty("_EmissionColorLDR")) mat.SetColor("_EmissionColorLDR", emissionColor);
        }

        /// <summary>
        /// Main Factory Method to build any 3D procedural mech piece.
        /// </summary>
        public static GameObject CreateMechPiece(PieceType type, PieceColor color, Transform parent = null)
        {
            if (_whiteArmorMat == null) InitializeMaterials();

            Material armorMat = (color == PieceColor.White) ? _whiteArmorMat : _blackArmorMat;
            Material glowMat  = (color == PieceColor.White) ? _whiteGlowMat : _blackGlowMat;

            string pieceName = $"{color}_{type}_Mech";
            GameObject root = new GameObject(pieceName);
            if (parent != null) root.transform.SetParent(parent, false);

            // 1. Check if a custom 3D Model (FBX/OBJ/Prefab) exists in Resources/Models/
            bool customModelLoaded = TryInstantiateCustomModel(type, color, root.transform, armorMat, glowMat);

            // 2. If no custom 3D model found, fall back to procedural geometry
            if (!customModelLoaded)
            {
                switch (type)
                {
                    case PieceType.Pawn:
                        BuildPawnScoutDrone(root.transform, armorMat, glowMat);
                        break;
                    case PieceType.Rook:
                        BuildRookSiegeBastion(root.transform, armorMat, glowMat);
                        break;
                    case PieceType.Knight:
                        BuildKnightStrikeStrider(root.transform, armorMat, glowMat);
                        break;
                    case PieceType.Bishop:
                        BuildBishopTacticalRelay(root.transform, armorMat, glowMat);
                        break;
                    case PieceType.Queen:
                        BuildQueenValkyrie(root.transform, armorMat, glowMat);
                        break;
                    case PieceType.King:
                        if (!BuildKingHologramStandee(root.transform, color, armorMat, glowMat))
                        {
                            BuildKingOverlordTitan(root.transform, armorMat, glowMat);
                        }
                        break;
                }
            }

            // Add Master Capsule Collider for selection and raycasting
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            float colHeight = type switch
            {
                PieceType.Pawn => 1.20f,
                PieceType.Rook => 1.40f,
                PieceType.Knight => 1.45f,
                PieceType.Bishop => 1.50f,
                PieceType.Queen => 1.70f,
                PieceType.King => 1.85f,
                _ => 1.40f
            };
            col.center = new Vector3(0, colHeight * 0.5f, 0);
            col.radius = (type == PieceType.King || type == PieceType.Queen) ? 0.45f : 0.38f;
            col.height = colHeight;

            // Attach dynamic animator for idle hovering, move trails, and destruction
            MechPieceAnimator animator = root.AddComponent<MechPieceAnimator>();
            animator.Initialize(color, glowMat.GetColor(glowMat.HasProperty("_EmissionColor") ? "_EmissionColor" : "_Color"));

            return root;
        }

        // =========================================================================
        // 1. PAWN - "Scout Drone"
        // Hexagonal/disc anti-grav base, angled armor torso, central laser eye, 2 side fins
        // =========================================================================
        private static void BuildPawnScoutDrone(Transform root, Material armor, Material glow)
        {
            // Hexagonal-style layered anti-grav base
            CreatePrimitive(PrimitiveType.Cylinder, "AntiGravBase", root, new Vector3(0, 0.08f, 0), new Vector3(0.55f, 0.07f, 0.55f), Quaternion.identity, _frameJointMat);
            CreatePrimitive(PrimitiveType.Cylinder, "BaseGlowRing", root, new Vector3(0, 0.12f, 0), new Vector3(0.48f, 0.03f, 0.48f), Quaternion.identity, glow);

            // Central Pylon
            CreatePrimitive(PrimitiveType.Cylinder, "Pylon", root, new Vector3(0, 0.28f, 0), new Vector3(0.2f, 0.15f, 0.2f), Quaternion.identity, _frameJointMat);

            // Angled Torso Chassis
            GameObject torso = CreatePrimitive(PrimitiveType.Cube, "TorsoChassis", root, new Vector3(0, 0.55f, 0), new Vector3(0.38f, 0.35f, 0.32f), Quaternion.Euler(15f, 0, 0), armor);

            // Laser Sensor Eye (Forward glowing sphere)
            CreatePrimitive(PrimitiveType.Sphere, "SensorLaserEye", torso.transform, new Vector3(0, 0.05f, 0.45f), new Vector3(0.35f, 0.35f, 0.35f), Quaternion.identity, glow);

            // 2 Side Radiator Cooling Fins
            CreatePrimitive(PrimitiveType.Cube, "RadiatorFin_L", torso.transform, new Vector3(-0.62f, 0.05f, -0.1f), new Vector3(0.08f, 0.45f, 0.38f), Quaternion.Euler(0, 20f, -15f), armor);
            CreatePrimitive(PrimitiveType.Cube, "RadiatorFin_R", torso.transform, new Vector3(0.62f, 0.05f, -0.1f), new Vector3(0.08f, 0.45f, 0.38f), Quaternion.Euler(0, -20f, 15f), armor);

            // Fin energy strips
            CreatePrimitive(PrimitiveType.Cube, "FinGlow_L", torso.transform, new Vector3(-0.65f, 0.05f, -0.1f), new Vector3(0.02f, 0.35f, 0.06f), Quaternion.Euler(0, 20f, -15f), glow);
            CreatePrimitive(PrimitiveType.Cube, "FinGlow_R", torso.transform, new Vector3(0.65f, 0.05f, -0.1f), new Vector3(0.02f, 0.35f, 0.06f), Quaternion.Euler(0, -20f, 15f), glow);
        }

        // =========================================================================
        // 2. ROOK - "Siege Bastion Mech"
        // Fortified base, 4 hydraulic corner pillar legs, heavy turret, twin cannons, energy shield
        // =========================================================================
        private static void BuildRookSiegeBastion(Transform root, Material armor, Material glow)
        {
            // Heavy Octagonal Platform Base
            CreatePrimitive(PrimitiveType.Cylinder, "BastionBase", root, new Vector3(0, 0.1f, 0), new Vector3(0.72f, 0.08f, 0.72f), Quaternion.identity, armor);

            // 4 Hydraulic Corner Pillar Legs
            Vector3[] legOffsets = {
                new Vector3(-0.32f, 0.28f, -0.32f),
                new Vector3( 0.32f, 0.28f, -0.32f),
                new Vector3(-0.32f, 0.28f,  0.32f),
                new Vector3( 0.32f, 0.28f,  0.32f)
            };
            for (int i = 0; i < 4; i++)
            {
                CreatePrimitive(PrimitiveType.Cylinder, $"HydraulicLeg_{i}", root, legOffsets[i], new Vector3(0.12f, 0.22f, 0.12f), Quaternion.identity, _frameJointMat);
                CreatePrimitive(PrimitiveType.Cube, $"LegArmor_{i}", root, legOffsets[i] + new Vector3(0, -0.05f, 0), new Vector3(0.16f, 0.18f, 0.16f), Quaternion.identity, armor);
            }

            // Heavy Bunker Body
            CreatePrimitive(PrimitiveType.Cube, "TurretBunker", root, new Vector3(0, 0.58f, 0), new Vector3(0.55f, 0.38f, 0.55f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cube, "ArmorPlatingFront", root, new Vector3(0, 0.58f, 0.29f), new Vector3(0.48f, 0.32f, 0.05f), Quaternion.Euler(-10f, 0, 0), armor);

            // Twin Artillery Barrels
            CreatePrimitive(PrimitiveType.Cylinder, "TwinCannon_L", root, new Vector3(-0.14f, 0.75f, 0.25f), new Vector3(0.09f, 0.32f, 0.09f), Quaternion.Euler(85f, 0, 0), _frameJointMat);
            CreatePrimitive(PrimitiveType.Cylinder, "TwinCannon_R", root, new Vector3( 0.14f, 0.75f, 0.25f), new Vector3(0.09f, 0.32f, 0.09f), Quaternion.Euler(85f, 0, 0), _frameJointMat);

            // Cannon Muzzle Energy Rings
            CreatePrimitive(PrimitiveType.Cylinder, "MuzzleGlow_L", root, new Vector3(-0.14f, 0.75f, 0.58f), new Vector3(0.11f, 0.02f, 0.11f), Quaternion.Euler(85f, 0, 0), glow);
            CreatePrimitive(PrimitiveType.Cylinder, "MuzzleGlow_R", root, new Vector3( 0.14f, 0.75f, 0.58f), new Vector3(0.11f, 0.02f, 0.11f), Quaternion.Euler(85f, 0, 0), glow);

            // Front Projected Energy Shield
            CreatePrimitive(PrimitiveType.Cube, "FrontEnergyShield", root, new Vector3(0, 0.65f, 0.45f), new Vector3(0.62f, 0.40f, 0.02f), Quaternion.Euler(-12f, 0, 0), glow);
        }

        // =========================================================================
        // 3. KNIGHT - "Strike Strider"
        // Bipedal reverse-joint walker legs, forward torso, angular cyber-steed head, glowing energy blade mane
        // =========================================================================
        private static void BuildKnightStrikeStrider(Transform root, Material armor, Material glow)
        {
            // Chassis Pelvis
            CreatePrimitive(PrimitiveType.Cube, "Pelvis", root, new Vector3(0, 0.35f, 0), new Vector3(0.32f, 0.12f, 0.25f), Quaternion.identity, _frameJointMat);

            // Reverse-joint Legs (Left & Right)
            float[] sides = { -0.20f, 0.20f };
            for (int i = 0; i < 2; i++)
            {
                float x = sides[i];
                // Thigh angled backward
                CreatePrimitive(PrimitiveType.Capsule, $"Thigh_{i}", root, new Vector3(x, 0.28f, -0.08f), new Vector3(0.08f, 0.16f, 0.08f), Quaternion.Euler(-35f, 0, 0), _frameJointMat);
                // Shin angled forward
                CreatePrimitive(PrimitiveType.Capsule, $"Shin_{i}", root, new Vector3(x, 0.12f, 0.04f), new Vector3(0.08f, 0.18f, 0.08f), Quaternion.Euler(30f, 0, 0), armor);
                // Talon Foot
                CreatePrimitive(PrimitiveType.Cube, $"Foot_{i}", root, new Vector3(x, 0.03f, 0.10f), new Vector3(0.12f, 0.05f, 0.24f), Quaternion.identity, _frameJointMat);
            }

            // Forward-thrust Torso
            Transform torso = CreatePrimitive(PrimitiveType.Cube, "Torso", root, new Vector3(0, 0.60f, 0.10f), new Vector3(0.35f, 0.32f, 0.42f), Quaternion.Euler(20f, 0, 0), armor).transform;

            // Cyber-Steed Neck & Angular Head
            Transform neck = CreatePrimitive(PrimitiveType.Cube, "MechNeck", torso, new Vector3(0, 0.32f, 0.22f), new Vector3(0.22f, 0.35f, 0.28f), Quaternion.Euler(35f, 0, 0), armor).transform;
            Transform head = CreatePrimitive(PrimitiveType.Cube, "SteedHead", neck, new Vector3(0, 0.26f, 0.16f), new Vector3(0.20f, 0.18f, 0.36f), Quaternion.Euler(-25f, 0, 0), armor).transform;

            // Cyber Visor / Optics
            CreatePrimitive(PrimitiveType.Cube, "CyberVisor", head, new Vector3(0, 0.02f, 0.52f), new Vector3(0.22f, 0.08f, 0.04f), Quaternion.identity, glow);

            // Energy Blade Mane (4 glowing dorsal blades along the neck ridge)
            for (int m = 0; m < 4; m++)
            {
                float zOffset = -0.3f + (m * 0.22f);
                float yOffset = 0.42f + (m * 0.08f);
                CreatePrimitive(PrimitiveType.Cube, $"ManeBlade_{m}", neck, new Vector3(0, yOffset, zOffset), new Vector3(0.04f, 0.22f, 0.14f), Quaternion.Euler(40f, 0, 0), glow);
            }
        }

        // =========================================================================
        // 4. BISHOP - "Tactical Relay Unit"
        // Tall slender chassis, 3 floating magnetic rings orbiting glowing crystal core, shoulder radar antennas
        // =========================================================================
        private static void BuildBishopTacticalRelay(Transform root, Material armor, Material glow)
        {
            // Pedestal Core Base
            CreatePrimitive(PrimitiveType.Cylinder, "RelayBase", root, new Vector3(0, 0.08f, 0), new Vector3(0.6f, 0.07f, 0.6f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cylinder, "SpirePillar", root, new Vector3(0, 0.42f, 0), new Vector3(0.22f, 0.35f, 0.22f), Quaternion.identity, _frameJointMat);

            // High Torso Relay Chassis
            Transform torso = CreatePrimitive(PrimitiveType.Cube, "RelayTorso", root, new Vector3(0, 0.85f, 0), new Vector3(0.32f, 0.42f, 0.28f), Quaternion.identity, armor).transform;

            // Chest Crystal Core (Glowing diamond core)
            CreatePrimitive(PrimitiveType.Sphere, "ResonanceCore", torso, new Vector3(0, 0, 0.25f), new Vector3(0.35f, 0.35f, 0.35f), Quaternion.identity, glow);

            // 3 Floating Magnetic Orbit Rings (Tilted rings around the core)
            CreatePrimitive(PrimitiveType.Cylinder, "OrbitRing_1", root, new Vector3(0, 0.85f, 0), new Vector3(0.68f, 0.025f, 0.68f), Quaternion.Euler(22f, 15f, 0), glow);
            CreatePrimitive(PrimitiveType.Cylinder, "OrbitRing_2", root, new Vector3(0, 0.85f, 0), new Vector3(0.58f, 0.022f, 0.58f), Quaternion.Euler(-18f, 45f, 12f), glow);
            CreatePrimitive(PrimitiveType.Cylinder, "OrbitRing_3", root, new Vector3(0, 0.85f, 0), new Vector3(0.48f, 0.020f, 0.48f), Quaternion.Euler(5f, -30f, -25f), glow);

            // Dual Shoulder Radar Targeting Antennas
            CreatePrimitive(PrimitiveType.Cylinder, "Antenna_L", torso, new Vector3(-0.65f, 0.45f, 0), new Vector3(0.04f, 0.35f, 0.04f), Quaternion.Euler(0, 0, -25f), _frameJointMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Antenna_R", torso, new Vector3( 0.65f, 0.45f, 0), new Vector3(0.04f, 0.35f, 0.04f), Quaternion.Euler(0, 0,  25f), _frameJointMat);

            CreatePrimitive(PrimitiveType.Sphere, "Dish_L", torso, new Vector3(-0.85f, 0.78f, 0), new Vector3(0.16f, 0.16f, 0.04f), Quaternion.Euler(0, 0, -25f), glow);
            CreatePrimitive(PrimitiveType.Sphere, "Dish_R", torso, new Vector3( 0.85f, 0.78f, 0), new Vector3(0.16f, 0.16f, 0.04f), Quaternion.Euler(0, 0,  25f), glow);

            // Mitre Crest Head
            ConeOrPointedCapsule("MitreHead", torso, new Vector3(0, 0.68f, 0), new Vector3(0.16f, 0.32f, 0.16f), armor);
        }

        // =========================================================================
        // 5. QUEEN - "Valkyrie Assault Mech"
        // Sleek aerodynamic frame, back jetpack wings, dual energy blades, optical crown
        // =========================================================================
        private static void BuildQueenValkyrie(Transform root, Material armor, Material glow)
        {
            // Sleek Anti-grav Booster Base
            CreatePrimitive(PrimitiveType.Cylinder, "BoosterBase", root, new Vector3(0, 0.08f, 0), new Vector3(0.58f, 0.06f, 0.58f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cylinder, "BoosterRing", root, new Vector3(0, 0.14f, 0), new Vector3(0.46f, 0.03f, 0.46f), Quaternion.identity, glow);

            // Slender Legs & Waist
            CreatePrimitive(PrimitiveType.Capsule, "Leg_L", root, new Vector3(-0.14f, 0.38f, 0), new Vector3(0.10f, 0.38f, 0.10f), Quaternion.identity, _frameJointMat);
            CreatePrimitive(PrimitiveType.Capsule, "Leg_R", root, new Vector3( 0.14f, 0.38f, 0), new Vector3(0.10f, 0.38f, 0.10f), Quaternion.identity, _frameJointMat);
            CreatePrimitive(PrimitiveType.Cube, "Waist", root, new Vector3(0, 0.65f, 0), new Vector3(0.32f, 0.16f, 0.22f), Quaternion.identity, armor);

            // Aerodynamic Torso
            Transform chest = CreatePrimitive(PrimitiveType.Cube, "ValkyrieChest", root, new Vector3(0, 0.88f, 0), new Vector3(0.38f, 0.32f, 0.26f), Quaternion.Euler(8f, 0, 0), armor).transform;
            CreatePrimitive(PrimitiveType.Cube, "ChestCore", chest, new Vector3(0, 0.05f, 0.38f), new Vector3(0.18f, 0.18f, 0.08f), Quaternion.identity, glow);

            // Jetpack & Spread Wings (Behind back)
            Transform jetpack = CreatePrimitive(PrimitiveType.Cube, "Jetpack", chest, new Vector3(0, 0.12f, -0.42f), new Vector3(0.30f, 0.28f, 0.16f), Quaternion.identity, _frameJointMat).transform;

            // Dual Thruster Nozzles
            CreatePrimitive(PrimitiveType.Cylinder, "Thruster_L", jetpack, new Vector3(-0.10f, -0.22f, 0), new Vector3(0.08f, 0.15f, 0.08f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cylinder, "Thruster_R", jetpack, new Vector3( 0.10f, -0.22f, 0), new Vector3(0.08f, 0.15f, 0.08f), Quaternion.identity, glow);

            // Spread Jet Wings (Left & Right)
            CreatePrimitive(PrimitiveType.Cube, "Wing_L", jetpack, new Vector3(-0.65f, 0.32f, 0), new Vector3(0.85f, 0.18f, 0.04f), Quaternion.Euler(15f, -15f, 32f), armor);
            CreatePrimitive(PrimitiveType.Cube, "Wing_R", jetpack, new Vector3( 0.65f, 0.32f, 0), new Vector3(0.85f, 0.18f, 0.04f), Quaternion.Euler(15f,  15f, -32f), armor);

            // Wing Energy Trailing Edges
            CreatePrimitive(PrimitiveType.Cube, "WingGlow_L", jetpack, new Vector3(-0.70f, 0.40f, 0), new Vector3(0.85f, 0.04f, 0.05f), Quaternion.Euler(15f, -15f, 32f), glow);
            CreatePrimitive(PrimitiveType.Cube, "WingGlow_R", jetpack, new Vector3( 0.70f, 0.40f, 0), new Vector3(0.85f, 0.04f, 0.05f), Quaternion.Euler(15f,  15f, -32f), glow);

            // Dual Energy Blades mounted on arms
            CreatePrimitive(PrimitiveType.Cube, "Blade_L", root, new Vector3(-0.38f, 0.65f, 0.22f), new Vector3(0.04f, 0.72f, 0.08f), Quaternion.Euler(35f, 0, 0), glow);
            CreatePrimitive(PrimitiveType.Cube, "Blade_R", root, new Vector3( 0.38f, 0.65f, 0.22f), new Vector3(0.04f, 0.72f, 0.08f), Quaternion.Euler(35f, 0, 0), glow);

            // Head & Sensor Crown
            Transform head = CreatePrimitive(PrimitiveType.Sphere, "Head", chest, new Vector3(0, 0.52f, 0), new Vector3(0.24f, 0.26f, 0.24f), Quaternion.identity, armor).transform;
            CreatePrimitive(PrimitiveType.Cube, "Visor", head, new Vector3(0, 0, 0.38f), new Vector3(0.22f, 0.06f, 0.04f), Quaternion.identity, glow);

            // Multi-point Sensor Crown
            CreatePrimitive(PrimitiveType.Cylinder, "CrownBase", head, new Vector3(0, 0.35f, 0), new Vector3(0.32f, 0.04f, 0.32f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cube, "CrownCrest_C", head, new Vector3(0, 0.48f, 0.08f), new Vector3(0.05f, 0.25f, 0.04f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cube, "CrownCrest_L", head, new Vector3(-0.12f, 0.42f, 0), new Vector3(0.04f, 0.18f, 0.04f), Quaternion.Euler(0, 0, 15f), glow);
            CreatePrimitive(PrimitiveType.Cube, "CrownCrest_R", head, new Vector3( 0.12f, 0.42f, 0), new Vector3(0.04f, 0.18f, 0.04f), Quaternion.Euler(0, 0, -15f), glow);
        }

        // =========================================================================
        // 6. KING - "Cyber Hologram AR Standee" (100% Concept Art Fidelity)
        // Renders the original high-resolution concept art as a glowing
        // double-sided holographic standee with ground neon pulse ring and beacons.
        // =========================================================================
        private static bool BuildKingHologramStandee(Transform root, PieceColor color, Material armorMat, Material glowMat)
        {
            string texName = (color == PieceColor.White) ? "Textures/King_White" : "Textures/King_Black";
            Texture2D tex = Resources.Load<Texture2D>(texName);
            if (tex == null) tex = Resources.Load<Texture2D>("Textures/King_Black");
            if (tex == null) return false;

            // Shader: prefer URP/Lit or Standard, fallback to Unlit/Transparent
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                         ?? Shader.Find("Standard") 
                         ?? Shader.Find("Unlit/Transparent")
                         ?? Shader.Find("Diffuse");

            Material standeeMat = new Material(shader) { name = $"{color}_King_HoloMat" };

            if (standeeMat.HasProperty("_BaseMap")) standeeMat.SetTexture("_BaseMap", tex);
            if (standeeMat.HasProperty("_MainTex")) standeeMat.SetTexture("_MainTex", tex);
            if (standeeMat.HasProperty("_BaseColor")) standeeMat.SetColor("_BaseColor", Color.white);
            if (standeeMat.HasProperty("_Color")) standeeMat.SetColor("_Color", Color.white);

            // Cutout configuration for razor-sharp alpha edges
            if (standeeMat.HasProperty("_Surface")) standeeMat.SetFloat("_Surface", 0); // Opaque with AlphaClip
            if (standeeMat.HasProperty("_AlphaClip")) standeeMat.SetFloat("_AlphaClip", 1.0f);
            if (standeeMat.HasProperty("_Cutoff")) standeeMat.SetFloat("_Cutoff", 0.20f);
            standeeMat.EnableKeyword("_ALPHATEST_ON");
            standeeMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            standeeMat.SetOverrideTag("RenderType", "TransparentCutout");

            // Double sided rendering
            if (standeeMat.HasProperty("_Cull")) standeeMat.SetFloat("_Cull", 0); // Cull Off

            // Subtle team neon emission glow on the hologram
            standeeMat.EnableKeyword("_EMISSION");
            Color teamGlow = (color == PieceColor.White) 
                ? new Color(0.0f, 0.9f, 1.2f) * 0.45f 
                : new Color(1.4f, 0.15f, 0.25f) * 0.45f;
            if (standeeMat.HasProperty("_EmissionColor")) standeeMat.SetColor("_EmissionColor", teamGlow);

            // Dimensions: Height 1.72m, Aspect Ratio 548/996 ≈ 0.55
            float standeeHeight = 1.72f;
            float standeeWidth = standeeHeight * (548f / 996f); // ~0.94m

            // Main Standee Pivot Container (handles smooth camera tracking)
            GameObject standeeContainer = new GameObject("HoloStandee_Container");
            standeeContainer.transform.SetParent(root, false);
            standeeContainer.transform.localPosition = Vector3.zero;

            // Front Quad
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "HoloCard_Front";
            quad.transform.SetParent(standeeContainer.transform, false);
            quad.transform.localPosition = new Vector3(0, standeeHeight * 0.5f, 0.005f);
            quad.transform.localScale = new Vector3(standeeWidth, standeeHeight, 1.0f);
            quad.transform.localRotation = Quaternion.identity;

            Collider qc = quad.GetComponent<Collider>();
            if (qc != null) UnityEngine.Object.Destroy(qc);
            Renderer qr = quad.GetComponent<Renderer>();
            if (qr != null) qr.sharedMaterial = standeeMat;

            // Back Quad (facing 180 deg so it renders cleanly from behind)
            GameObject backQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backQuad.name = "HoloCard_Back";
            backQuad.transform.SetParent(standeeContainer.transform, false);
            backQuad.transform.localPosition = new Vector3(0, standeeHeight * 0.5f, -0.005f);
            backQuad.transform.localScale = new Vector3(standeeWidth, standeeHeight, 1.0f);
            backQuad.transform.localRotation = Quaternion.Euler(0, 180f, 0);

            Collider bqc = backQuad.GetComponent<Collider>();
            if (bqc != null) UnityEngine.Object.Destroy(bqc);
            Renderer bqr = backQuad.GetComponent<Renderer>();
            if (bqr != null) bqr.sharedMaterial = standeeMat;

            // Camera Y-Axis Face Script: Always rotates toward Camera Y so King looks magnificent from any AR angle!
            standeeContainer.AddComponent<HologramStandeeBillboard>();

            // Ground Cyber Holographic Pedestal (clean neon pulse ring at floor level)
            CreatePrimitive(PrimitiveType.Cylinder, "HoloRing_Pulse", root, new Vector3(0, 0.02f, 0), new Vector3(0.72f, 0.015f, 0.72f), Quaternion.identity, glowMat);
            CreatePrimitive(PrimitiveType.Cylinder, "HoloRing_Core", root, new Vector3(0, 0.01f, 0), new Vector3(0.58f, 0.01f, 0.58f), Quaternion.identity, armorMat);

            // 4 Cyber Hologram Emitter Beacons around the base
            float[] nodeAngles = { 45f, 135f, 225f, 315f };
            for (int i = 0; i < 4; i++)
            {
                float rad = nodeAngles[i] * Mathf.Deg2Rad;
                float nx = Mathf.Sin(rad) * 0.34f;
                float nz = Mathf.Cos(rad) * 0.34f;
                CreatePrimitive(PrimitiveType.Cube, $"HoloEmitter_{i}", root, new Vector3(nx, 0.035f, nz), new Vector3(0.06f, 0.05f, 0.06f), Quaternion.identity, glowMat);
            }

            return true;
        }

        // =========================================================================
        // 6b. KING - "Overlord Titan" Procedural Fallback
        // =========================================================================
        private static void BuildKingOverlordTitan(Transform root, Material armor, Material glow)
        {
            // ── SECTION 1: HEAVY FORTIFIED LEGS & COMBAT MECH FEET ──
            // Autonomous bipedal mech standing directly on the board (no pedestal)
            float[] legSides = { -0.22f, 0.22f };
            for (int i = 0; i < 2; i++)
            {
                float x = legSides[i];
                // Upper thigh armor
                CreatePrimitive(PrimitiveType.Cube, $"TitanThigh_{i}", root, new Vector3(x, 0.38f, 0), new Vector3(0.20f, 0.35f, 0.24f), Quaternion.identity, armor);
                // Thigh hydraulic piston (back of leg)
                CreatePrimitive(PrimitiveType.Cylinder, $"ThighPiston_{i}", root, new Vector3(x, 0.36f, -0.10f), new Vector3(0.06f, 0.22f, 0.06f), Quaternion.identity, _frameJointMat);
                // Knee guard plate (angled)
                CreatePrimitive(PrimitiveType.Cube, $"KneeGuard_{i}", root, new Vector3(x, 0.28f, 0.12f), new Vector3(0.16f, 0.14f, 0.08f), Quaternion.Euler(-15f, 0, 0), armor);
                // Knee glow strip
                CreatePrimitive(PrimitiveType.Cube, $"KneeGlow_{i}", root, new Vector3(x, 0.26f, 0.14f), new Vector3(0.10f, 0.03f, 0.04f), Quaternion.Euler(-15f, 0, 0), glow);
                // Lower shin armor
                CreatePrimitive(PrimitiveType.Cube, $"TitanShin_{i}", root, new Vector3(x, 0.18f, 0.02f), new Vector3(0.17f, 0.20f, 0.20f), Quaternion.identity, _frameJointMat);
                // Shin armor front plate
                CreatePrimitive(PrimitiveType.Cube, $"ShinPlate_{i}", root, new Vector3(x, 0.18f, 0.12f), new Vector3(0.14f, 0.16f, 0.04f), Quaternion.Euler(5f, 0, 0), armor);
                // Ankle mechanical joint
                CreatePrimitive(PrimitiveType.Sphere, $"AnkleJoint_{i}", root, new Vector3(x, 0.08f, 0), new Vector3(0.12f, 0.12f, 0.12f), Quaternion.identity, _frameJointMat);
                // Armored foot / boot resting flat on the ground (bottom at y=0)
                CreatePrimitive(PrimitiveType.Cube, $"TitanFoot_{i}", root, new Vector3(x, 0.04f, 0.05f), new Vector3(0.18f, 0.08f, 0.28f), Quaternion.identity, armor);
                // Foot toe guard
                CreatePrimitive(PrimitiveType.Cube, $"ToeGuard_{i}", root, new Vector3(x, 0.03f, 0.18f), new Vector3(0.16f, 0.06f, 0.08f), Quaternion.Euler(15f, 0, 0), armor);
                // Heel stabilizer spur
                CreatePrimitive(PrimitiveType.Cube, $"HeelSpur_{i}", root, new Vector3(x, 0.04f, -0.10f), new Vector3(0.14f, 0.06f, 0.08f), Quaternion.identity, _frameJointMat);
                // Energy channel sole glow
                CreatePrimitive(PrimitiveType.Cube, $"FootGlow_{i}", root, new Vector3(x, 0.02f, 0.06f), new Vector3(0.04f, 0.02f, 0.16f), Quaternion.identity, glow);
            }

            // ── SECTION 3: FORTIFIED PELVIS & WAIST ──
            CreatePrimitive(PrimitiveType.Cube, "PelvisArmor", root, new Vector3(0, 0.58f, 0), new Vector3(0.55f, 0.18f, 0.35f), Quaternion.identity, _frameJointMat);
            // Waist belt with energy channel
            CreatePrimitive(PrimitiveType.Cube, "WaistBelt", root, new Vector3(0, 0.52f, 0), new Vector3(0.58f, 0.06f, 0.38f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cube, "WaistGlow", root, new Vector3(0, 0.52f, 0.18f), new Vector3(0.30f, 0.03f, 0.04f), Quaternion.identity, glow);
            // Front tasset armor plates (hanging armor skirts)
            CreatePrimitive(PrimitiveType.Cube, "Tasset_L", root, new Vector3(-0.18f, 0.45f, 0.14f), new Vector3(0.14f, 0.16f, 0.04f), Quaternion.Euler(8f, 0, 5f), armor);
            CreatePrimitive(PrimitiveType.Cube, "Tasset_R", root, new Vector3(0.18f, 0.45f, 0.14f), new Vector3(0.14f, 0.16f, 0.04f), Quaternion.Euler(8f, 0, -5f), armor);
            CreatePrimitive(PrimitiveType.Cube, "Tasset_C", root, new Vector3(0, 0.44f, 0.16f), new Vector3(0.12f, 0.20f, 0.04f), Quaternion.Euler(10f, 0, 0), armor);

            // ── SECTION 4: MASSIVE UPPER TORSO ──
            Transform torso = CreatePrimitive(PrimitiveType.Cube, "OverlordTorso", root, new Vector3(0, 0.88f, 0), new Vector3(0.65f, 0.48f, 0.42f), Quaternion.identity, armor).transform;
            // Upper chest plate (angled forward for aggressive posture)
            CreatePrimitive(PrimitiveType.Cube, "ChestPlateUpper", torso, new Vector3(0, 0.18f, 0.22f), new Vector3(0.50f, 0.22f, 0.06f), Quaternion.Euler(-12f, 0, 0), armor);
            // Collar gorget armor
            CreatePrimitive(PrimitiveType.Cube, "CollarGorget", torso, new Vector3(0, 0.32f, 0.10f), new Vector3(0.42f, 0.08f, 0.28f), Quaternion.Euler(-5f, 0, 0), armor);
            // Back spine ridge
            CreatePrimitive(PrimitiveType.Cube, "SpineRidge", torso, new Vector3(0, 0.10f, -0.24f), new Vector3(0.10f, 0.38f, 0.06f), Quaternion.identity, _frameJointMat);

            // ── SECTION 5: NUCLEAR REACTOR CORE (Chest) ──
            // Containment frame and intensely glowing reactor sphere
            CreatePrimitive(PrimitiveType.Cylinder, "ReactorFrame_Outer", torso, new Vector3(0, 0.02f, 0.42f), new Vector3(0.38f, 0.04f, 0.38f), Quaternion.Euler(90f, 0, 0), _frameJointMat);
            CreatePrimitive(PrimitiveType.Cylinder, "ReactorFrame_Inner", torso, new Vector3(0, 0.02f, 0.44f), new Vector3(0.28f, 0.03f, 0.28f), Quaternion.Euler(90f, 0, 0), glow);
            CreatePrimitive(PrimitiveType.Sphere, "NuclearReactorCore", torso, new Vector3(0, 0.02f, 0.46f), new Vector3(0.30f, 0.30f, 0.30f), Quaternion.identity, glow);
            // Reactor energy veins (radiating lines from core)
            CreatePrimitive(PrimitiveType.Cube, "ReactorVein_Up", torso, new Vector3(0, 0.20f, 0.42f), new Vector3(0.025f, 0.18f, 0.03f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cube, "ReactorVein_Down", torso, new Vector3(0, -0.16f, 0.42f), new Vector3(0.025f, 0.14f, 0.03f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cube, "ReactorVein_L", torso, new Vector3(-0.18f, 0.02f, 0.42f), new Vector3(0.14f, 0.025f, 0.03f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cube, "ReactorVein_R", torso, new Vector3(0.18f, 0.02f, 0.42f), new Vector3(0.14f, 0.025f, 0.03f), Quaternion.identity, glow);

            // ── SECTION 6: MASSIVE SPIKED SHOULDER PAULDRONS ──
            float[] shoulderSides = { -1f, 1f };
            string[] sideNames = { "L", "R" };
            for (int i = 0; i < 2; i++)
            {
                float x = shoulderSides[i] * 0.52f;
                float sign = shoulderSides[i];

                // Main pauldron body (large, angular)
                Transform pauldron = CreatePrimitive(PrimitiveType.Cube, $"Pauldron_{sideNames[i]}", torso, new Vector3(x, 0.28f, 0), new Vector3(0.42f, 0.30f, 0.40f), Quaternion.Euler(0, 0, sign * -8f), armor).transform;

                // Upper pauldron extension (raised spike guard)
                CreatePrimitive(PrimitiveType.Cube, $"PauldronUpper_{sideNames[i]}", pauldron, new Vector3(sign * 0.10f, 0.28f, 0), new Vector3(0.32f, 0.12f, 0.34f), Quaternion.Euler(0, 0, sign * -12f), armor);

                // Pauldron edge spikes (3 spikes each side, pointing up and outward)
                for (int s = 0; s < 3; s++)
                {
                    float zOff = -0.16f + (s * 0.16f);
                    float spikeAngle = 15f + (s * 8f);
                    CreatePrimitive(PrimitiveType.Cube, $"PauldronSpike_{sideNames[i]}_{s}", pauldron,
                        new Vector3(sign * 0.22f, 0.32f + (s * 0.06f), zOff),
                        new Vector3(0.04f, 0.22f, 0.04f),
                        Quaternion.Euler(-spikeAngle, 0, sign * -25f), armor);
                }

                // Pauldron glow trim
                CreatePrimitive(PrimitiveType.Cube, $"PauldronGlowTrim_{sideNames[i]}", pauldron, new Vector3(0, -0.16f, 0.20f), new Vector3(0.36f, 0.03f, 0.04f), Quaternion.identity, glow);

                // Missile pod cluster (4 tubes)
                CreatePrimitive(PrimitiveType.Cylinder, $"MissileTube_{sideNames[i]}_0", pauldron, new Vector3(-0.08f,  0.06f, 0.42f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0, 0), glow);
                CreatePrimitive(PrimitiveType.Cylinder, $"MissileTube_{sideNames[i]}_1", pauldron, new Vector3( 0.08f,  0.06f, 0.42f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0, 0), glow);
                CreatePrimitive(PrimitiveType.Cylinder, $"MissileTube_{sideNames[i]}_2", pauldron, new Vector3(-0.08f, -0.06f, 0.42f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0, 0), glow);
                CreatePrimitive(PrimitiveType.Cylinder, $"MissileTube_{sideNames[i]}_3", pauldron, new Vector3( 0.08f, -0.06f, 0.42f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0, 0), glow);
            }

            // ── SECTION 7: ARMORED ARMS WITH ENERGY WEAPONS ──
            for (int i = 0; i < 2; i++)
            {
                float x = shoulderSides[i] * 0.42f;
                float sign = shoulderSides[i];

                // Upper arm
                CreatePrimitive(PrimitiveType.Cube, $"UpperArm_{sideNames[i]}", torso, new Vector3(x, 0.02f, 0), new Vector3(0.12f, 0.30f, 0.14f), Quaternion.Euler(0, 0, sign * 8f), _frameJointMat);
                // Elbow joint
                CreatePrimitive(PrimitiveType.Sphere, $"ElbowJoint_{sideNames[i]}", torso, new Vector3(x * 1.05f, -0.16f, 0), new Vector3(0.10f, 0.10f, 0.10f), Quaternion.identity, _frameJointMat);
                // Forearm armor
                CreatePrimitive(PrimitiveType.Cube, $"Forearm_{sideNames[i]}", torso, new Vector3(x * 1.10f, -0.32f, 0.08f), new Vector3(0.14f, 0.26f, 0.16f), Quaternion.Euler(15f, 0, sign * 5f), armor);
                // Forearm glow strip
                CreatePrimitive(PrimitiveType.Cube, $"ForearmGlow_{sideNames[i]}", torso, new Vector3(x * 1.12f, -0.32f, 0.17f), new Vector3(0.03f, 0.20f, 0.03f), Quaternion.Euler(15f, 0, sign * 5f), glow);
                // Gauntlet
                CreatePrimitive(PrimitiveType.Cube, $"Gauntlet_{sideNames[i]}", torso, new Vector3(x * 1.12f, -0.48f, 0.12f), new Vector3(0.11f, 0.10f, 0.14f), Quaternion.identity, armor);
            }

            // Right arm: Energy Greatsword (large blade)
            Transform swordRoot = new GameObject("Greatsword").transform;
            swordRoot.SetParent(torso, false);
            swordRoot.localPosition = new Vector3(0.50f, -0.52f, 0.18f);
            swordRoot.localRotation = Quaternion.Euler(25f, 0, -5f);
            // Sword handle
            CreatePrimitive(PrimitiveType.Cylinder, "SwordHandle", swordRoot, Vector3.zero, new Vector3(0.04f, 0.12f, 0.04f), Quaternion.identity, _frameJointMat);
            // Sword guard
            CreatePrimitive(PrimitiveType.Cube, "SwordGuard", swordRoot, new Vector3(0, 0.14f, 0), new Vector3(0.16f, 0.03f, 0.06f), Quaternion.identity, armor);
            // Sword blade (long glowing)
            CreatePrimitive(PrimitiveType.Cube, "SwordBlade", swordRoot, new Vector3(0, 0.52f, 0), new Vector3(0.06f, 0.65f, 0.03f), Quaternion.identity, glow);
            // Sword blade edge glow
            CreatePrimitive(PrimitiveType.Cube, "SwordEdge", swordRoot, new Vector3(0.035f, 0.52f, 0), new Vector3(0.015f, 0.60f, 0.035f), Quaternion.identity, glow);

            // Left arm: Shield or secondary blade
            CreatePrimitive(PrimitiveType.Cube, "ArmBlade_L", torso, new Vector3(-0.52f, -0.50f, 0.20f), new Vector3(0.04f, 0.50f, 0.06f), Quaternion.Euler(30f, 0, 5f), glow);

            // ── SECTION 8: WARRIOR HELMET HEAD ──
            // Neck armor
            CreatePrimitive(PrimitiveType.Cylinder, "NeckArmor", torso, new Vector3(0, 0.38f, 0.02f), new Vector3(0.18f, 0.08f, 0.18f), Quaternion.identity, _frameJointMat);

            Transform head = CreatePrimitive(PrimitiveType.Cube, "TitanHead", torso, new Vector3(0, 0.52f, 0.04f), new Vector3(0.26f, 0.24f, 0.26f), Quaternion.identity, armor).transform;
            // Helmet faceplate
            CreatePrimitive(PrimitiveType.Cube, "HelmetFace", head, new Vector3(0, -0.02f, 0.14f), new Vector3(0.22f, 0.20f, 0.06f), Quaternion.Euler(-5f, 0, 0), armor);
            // T-shaped Visor (glowing optic slit)
            CreatePrimitive(PrimitiveType.Cube, "VisorHorizontal", head, new Vector3(0, 0.04f, 0.42f), new Vector3(0.22f, 0.04f, 0.03f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cube, "VisorVertical", head, new Vector3(0, -0.02f, 0.42f), new Vector3(0.04f, 0.12f, 0.03f), Quaternion.identity, glow);
            // Helmet cheek guards
            CreatePrimitive(PrimitiveType.Cube, "CheekGuard_L", head, new Vector3(-0.14f, -0.04f, 0.10f), new Vector3(0.06f, 0.16f, 0.18f), Quaternion.Euler(0, 8f, 0), armor);
            CreatePrimitive(PrimitiveType.Cube, "CheekGuard_R", head, new Vector3(0.14f, -0.04f, 0.10f), new Vector3(0.06f, 0.16f, 0.18f), Quaternion.Euler(0, -8f, 0), armor);
            // Helmet crest ridge (central)
            CreatePrimitive(PrimitiveType.Cube, "HelmetCrest", head, new Vector3(0, 0.14f, -0.02f), new Vector3(0.04f, 0.06f, 0.28f), Quaternion.identity, armor);

            // ── SECTION 9: CROWN OF THORNS (Multi-layered, ornate) ──
            Transform crownRoot = new GameObject("CrownOfThorns").transform;
            crownRoot.SetParent(head, false);
            crownRoot.localPosition = new Vector3(0, 0.20f, 0);

            // Crown base ring
            CreatePrimitive(PrimitiveType.Cylinder, "CrownBaseRing", crownRoot, Vector3.zero, new Vector3(0.32f, 0.03f, 0.32f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cylinder, "CrownGlowRing", crownRoot, new Vector3(0, 0.03f, 0), new Vector3(0.30f, 0.015f, 0.30f), Quaternion.identity, glow);

            // 8 Crown spires radiating outward (main thorns)
            for (int s = 0; s < 8; s++)
            {
                float angle = s * 45f * Mathf.Deg2Rad;
                float cx = Mathf.Sin(angle) * 0.14f;
                float cz = Mathf.Cos(angle) * 0.14f;
                float height = (s % 2 == 0) ? 0.30f : 0.22f; // Alternating heights
                float tiltX = Mathf.Cos(angle) * -12f;
                float tiltZ = Mathf.Sin(angle) * 12f;

                CreatePrimitive(PrimitiveType.Cube, $"CrownSpire_{s}", crownRoot,
                    new Vector3(cx, height * 0.5f + 0.04f, cz),
                    new Vector3(0.035f, height, 0.035f),
                    Quaternion.Euler(tiltX, s * 45f, tiltZ), armor);

                // Spire tip glow
                CreatePrimitive(PrimitiveType.Sphere, $"SpireTip_{s}", crownRoot,
                    new Vector3(cx * 1.15f, height + 0.06f, cz * 1.15f),
                    new Vector3(0.04f, 0.04f, 0.04f),
                    Quaternion.identity, glow);
            }

            // Secondary inner crown thorn ring (smaller, more vertical)
            for (int s = 0; s < 4; s++)
            {
                float angle = (s * 90f + 22.5f) * Mathf.Deg2Rad;
                float cx = Mathf.Sin(angle) * 0.08f;
                float cz = Mathf.Cos(angle) * 0.08f;
                CreatePrimitive(PrimitiveType.Cube, $"InnerThorn_{s}", crownRoot,
                    new Vector3(cx, 0.22f, cz),
                    new Vector3(0.025f, 0.20f, 0.025f),
                    Quaternion.Euler(0, s * 90f + 22.5f, 0), glow);
            }

            // ── SECTION 10: MECHANICAL CROSS CREST (Ornate, atop crown) ──
            Transform crossRoot = new GameObject("TitanCrossCrest").transform;
            crossRoot.SetParent(crownRoot, false);
            crossRoot.localPosition = new Vector3(0, 0.36f, 0);

            // Main cross structure (thick, imposing)
            CreatePrimitive(PrimitiveType.Cube, "CrossVertical", crossRoot, Vector3.zero, new Vector3(0.07f, 0.38f, 0.05f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cube, "CrossHorizontal", crossRoot, new Vector3(0, 0.08f, 0), new Vector3(0.26f, 0.07f, 0.05f), Quaternion.identity, glow);
            // Cross armor backing (darker frame behind the glow)
            CreatePrimitive(PrimitiveType.Cube, "CrossBackV", crossRoot, new Vector3(0, 0, -0.03f), new Vector3(0.09f, 0.40f, 0.03f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cube, "CrossBackH", crossRoot, new Vector3(0, 0.08f, -0.03f), new Vector3(0.28f, 0.09f, 0.03f), Quaternion.identity, armor);
            // Cross tip ornaments (small spheres at each end)
            CreatePrimitive(PrimitiveType.Sphere, "CrossTip_Top", crossRoot, new Vector3(0, 0.22f, 0), new Vector3(0.06f, 0.06f, 0.06f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Sphere, "CrossTip_Bottom", crossRoot, new Vector3(0, -0.18f, 0), new Vector3(0.05f, 0.05f, 0.05f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Sphere, "CrossTip_Left", crossRoot, new Vector3(-0.15f, 0.08f, 0), new Vector3(0.05f, 0.05f, 0.05f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Sphere, "CrossTip_Right", crossRoot, new Vector3(0.15f, 0.08f, 0), new Vector3(0.05f, 0.05f, 0.05f), Quaternion.identity, glow);

            // ── SECTION 11: GOTHIC CAPE / TATTERED CLOAK ──
            // Main cape body (flowing behind the titan)
            Transform capeRoot = new GameObject("GothicCape").transform;
            capeRoot.SetParent(torso, false);
            capeRoot.localPosition = new Vector3(0, 0.10f, -0.26f);

            // Cape collar attachment
            CreatePrimitive(PrimitiveType.Cube, "CapeCollar", capeRoot, new Vector3(0, 0.22f, 0), new Vector3(0.52f, 0.08f, 0.06f), Quaternion.Euler(10f, 0, 0), armor);

            // Main cape panels (wide, flowing downward)
            CreatePrimitive(PrimitiveType.Cube, "CapeMain", capeRoot, new Vector3(0, -0.20f, -0.06f), new Vector3(0.58f, 0.72f, 0.04f), Quaternion.Euler(8f, 0, 0), armor);
            CreatePrimitive(PrimitiveType.Cube, "CapeInner", capeRoot, new Vector3(0, -0.22f, -0.02f), new Vector3(0.52f, 0.68f, 0.03f), Quaternion.Euler(6f, 0, 0), _frameJointMat);

            // Tattered cape strips (ragged bottom edges for gothic effect - different lengths)
            float[] stripX = { -0.24f, -0.14f, -0.04f, 0.06f, 0.16f, 0.26f };
            float[] stripH = { 0.28f, 0.35f, 0.22f, 0.32f, 0.26f, 0.30f };
            for (int t = 0; t < 6; t++)
            {
                float bottomY = -0.58f - stripH[t] * 0.5f;
                // Armor strips
                CreatePrimitive(PrimitiveType.Cube, $"CapeStrip_{t}", capeRoot,
                    new Vector3(stripX[t], bottomY, -0.08f - (t % 2) * 0.03f),
                    new Vector3(0.08f, stripH[t], 0.025f),
                    Quaternion.Euler(5f + t * 2f, t * 3f - 8f, t * 2f - 5f), armor);

                // Every other strip has glow edge (crimson/cyan energy tatter)
                if (t % 2 == 0)
                {
                    CreatePrimitive(PrimitiveType.Cube, $"CapeGlowStrip_{t}", capeRoot,
                        new Vector3(stripX[t] + 0.01f, bottomY - 0.02f, -0.10f - (t % 2) * 0.03f),
                        new Vector3(0.06f, stripH[t] * 0.6f, 0.015f),
                        Quaternion.Euler(8f + t * 2f, t * 3f - 8f, t * 2f - 5f), glow);
                }
            }

            // Side cape drape extensions (flowing over shoulders)
            CreatePrimitive(PrimitiveType.Cube, "CapeDrape_L", capeRoot, new Vector3(-0.32f, 0.02f, 0.02f), new Vector3(0.10f, 0.50f, 0.04f), Quaternion.Euler(5f, 10f, 12f), armor);
            CreatePrimitive(PrimitiveType.Cube, "CapeDrape_R", capeRoot, new Vector3(0.32f, 0.02f, 0.02f), new Vector3(0.10f, 0.50f, 0.04f), Quaternion.Euler(5f, -10f, -12f), armor);

            // ── SECTION 12: SIDE BANNERS / PENNANTS ──
            // Hanging war banners from shoulder pauldrons
            for (int i = 0; i < 2; i++)
            {
                float bx = shoulderSides[i] * 0.58f;
                float sign = shoulderSides[i];
                // Banner pole
                CreatePrimitive(PrimitiveType.Cylinder, $"BannerPole_{sideNames[i]}", torso, new Vector3(bx, 0.38f, -0.18f), new Vector3(0.02f, 0.30f, 0.02f), Quaternion.Euler(0, 0, sign * -5f), _frameJointMat);
                // Banner fabric
                CreatePrimitive(PrimitiveType.Cube, $"Banner_{sideNames[i]}", torso, new Vector3(bx * 1.02f, 0.10f, -0.20f), new Vector3(0.14f, 0.35f, 0.02f), Quaternion.Euler(3f, sign * 5f, sign * -3f), glow);
                // Banner tear
                CreatePrimitive(PrimitiveType.Cube, $"BannerTear_{sideNames[i]}", torso, new Vector3(bx * 1.04f, -0.12f, -0.22f), new Vector3(0.10f, 0.12f, 0.015f), Quaternion.Euler(8f, sign * 8f, sign * -6f), armor);
            }
        }

        /// <summary>
        /// Checks if a custom 3D model (.glb, .fbx, .obj, .prefab) exists in Assets/Resources/Models/
        /// organized by team color folders (Red/ for Black, Blue/ for White).
        /// Instantiates with normalized scale, proportional height, and grounded positioning.
        /// </summary>
        private static bool TryInstantiateCustomModel(PieceType type, PieceColor color, Transform root, Material armorMat, Material glowMat)
        {
            string colorFolder = (color == PieceColor.White) ? "Blue" : "Red";
            string altColorFolder = (color == PieceColor.White) ? "White" : "Black";

            string[] possiblePaths = {
                $"Models/{colorFolder}/{type}",
                $"Models/{colorFolder}/{color}_{type}",
                $"Models/{colorFolder}/{type}_{color}",
                $"Models/{altColorFolder}/{type}",
                $"Models/{altColorFolder}/{color}_{type}",
                $"Models/{color}_{type}",
                $"Models/{color}_{type}_Model",
                $"Models/{type}_{color}",
                $"Models/{type}",
                $"Models/{type}_Model",
                $"Models/{color}_{type}_Mech"
            };

            GameObject prefab = null;
            foreach (var path in possiblePaths)
            {
                prefab = Resources.Load<GameObject>(path);
                if (prefab != null) break;
            }

            if (prefab == null) return false;

            GameObject modelInstance = UnityEngine.Object.Instantiate(prefab, root, false);
            modelInstance.name = $"{type}_3DModel";

            // Clean up any inner colliders so only root master collider handles selection
            Collider[] colliders = modelInstance.GetComponentsInChildren<Collider>();
            foreach (var c in colliders) UnityEngine.Object.Destroy(c);

            // Auto-scale model if needed so it fits nicely on a chess tile with proper proportional hierarchy
            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                float currentHeight = bounds.size.y;
                if (currentHeight > 0.05f)
                {
                    float targetHeight = type switch
                    {
                        PieceType.Pawn => 1.15f,
                        PieceType.Rook => 1.35f,
                        PieceType.Knight => 1.40f,
                        PieceType.Bishop => 1.45f,
                        PieceType.Queen => 1.65f,
                        PieceType.King => 1.75f,
                        _ => 1.35f
                    };

                    if (Mathf.Abs(currentHeight - targetHeight) > 0.05f)
                    {
                        float scaleFactor = targetHeight / currentHeight;
                        modelInstance.transform.localScale *= scaleFactor;
                    }

                    // Re-calculate bounds after scaling to align feet with y=0
                    bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    float yMin = bounds.min.y - root.position.y;
                    if (Mathf.Abs(yMin) > 0.02f)
                    {
                        modelInstance.transform.localPosition = new Vector3(0, -yMin, 0);
                    }
                }
            }

            // Add sleek team aura ring at the feet (Cyan for White, Crimson for Black) to match sci-fi chess theme
            CreatePrimitive(PrimitiveType.Cylinder, "TeamAuraRing", root, new Vector3(0, 0.015f, 0), new Vector3(0.68f, 0.012f, 0.68f), Quaternion.identity, glowMat);

            return true;
        }

        // =========================================================================
        // Helper Methods for Geometric Primitive Assembly
        // =========================================================================
        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Quaternion localRot, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            obj.transform.localRotation = localRot;

            // Remove default sub-colliders so only root master collider handles physics/clicks
            Collider c = obj.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.Destroy(c);

            Renderer r = obj.GetComponent<Renderer>();
            if (r != null && mat != null)
            {
                r.sharedMaterial = mat;
            }

            return obj;
        }

        private static GameObject ConeOrPointedCapsule(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;

            Collider c = obj.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.Destroy(c);

            Renderer r = obj.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;

            return obj;
        }
    }
}
