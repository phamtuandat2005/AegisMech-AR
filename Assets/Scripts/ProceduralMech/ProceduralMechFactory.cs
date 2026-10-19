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

            // Assemble procedural hierarchy based on PieceType
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
                    BuildKingOverlordTitan(root.transform, armorMat, glowMat);
                    break;
            }

            // Add Master Capsule Collider for selection and raycasting
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.65f, 0);
            col.radius = 0.35f;
            col.height = 1.35f;

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
        // 6. KING - "Overlord Titan"
        // Colossal titan chassis, missile pods, pulsating nuclear reactor core, glowing mechanical cross crest
        // =========================================================================
        private static void BuildKingOverlordTitan(Transform root, Material armor, Material glow)
        {
            // Reinforced Titan Platform Base
            CreatePrimitive(PrimitiveType.Cylinder, "TitanBasePlatform", root, new Vector3(0, 0.10f, 0), new Vector3(0.78f, 0.09f, 0.78f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cylinder, "BaseGlowRing", root, new Vector3(0, 0.15f, 0), new Vector3(0.65f, 0.04f, 0.65f), Quaternion.identity, glow);

            // Heavy Fortified Legs & Pelvis
            CreatePrimitive(PrimitiveType.Cube, "TitanLeg_L", root, new Vector3(-0.24f, 0.38f, 0), new Vector3(0.20f, 0.45f, 0.25f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cube, "TitanLeg_R", root, new Vector3( 0.24f, 0.38f, 0), new Vector3(0.20f, 0.45f, 0.25f), Quaternion.identity, armor);
            CreatePrimitive(PrimitiveType.Cube, "PelvisArmor", root, new Vector3(0, 0.65f, 0), new Vector3(0.55f, 0.22f, 0.38f), Quaternion.identity, _frameJointMat);

            // Massive Armored Upper Torso
            Transform torso = CreatePrimitive(PrimitiveType.Cube, "OverlordTorso", root, new Vector3(0, 1.05f, 0), new Vector3(0.68f, 0.52f, 0.48f), Quaternion.identity, armor).transform;

            // Chest Arc/Reactor Core (Intensely glowing sphere + containment collar)
            CreatePrimitive(PrimitiveType.Cylinder, "ReactorCollar", torso, new Vector3(0, 0.02f, 0.52f), new Vector3(0.42f, 0.05f, 0.42f), Quaternion.Euler(90f, 0, 0), _frameJointMat);
            CreatePrimitive(PrimitiveType.Sphere, "NuclearReactorCore", torso, new Vector3(0, 0.02f, 0.54f), new Vector3(0.38f, 0.38f, 0.38f), Quaternion.identity, glow);

            // Massive Shoulder Pauldrons with Mini-Missile Pods (Left & Right)
            float[] shoulderSides = { -0.75f, 0.75f };
            for (int i = 0; i < 2; i++)
            {
                float x = shoulderSides[i];
                Transform pauldron = CreatePrimitive(PrimitiveType.Cube, $"Pauldron_{i}", torso, new Vector3(x, 0.38f, 0), new Vector3(0.38f, 0.35f, 0.45f), Quaternion.identity, armor).transform;

                // Honeycomb Missile Pods (4 tubes each)
                CreatePrimitive(PrimitiveType.Cylinder, $"MissileTube_0", pauldron, new Vector3(-0.08f,  0.08f, 0.48f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0, 0), glow);
                CreatePrimitive(PrimitiveType.Cylinder, $"MissileTube_1", pauldron, new Vector3( 0.08f,  0.08f, 0.48f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0, 0), glow);
                CreatePrimitive(PrimitiveType.Cylinder, $"MissileTube_2", pauldron, new Vector3(-0.08f, -0.08f, 0.48f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0, 0), glow);
                CreatePrimitive(PrimitiveType.Cylinder, $"MissileTube_3", pauldron, new Vector3( 0.08f, -0.08f, 0.48f), new Vector3(0.06f, 0.08f, 0.06f), Quaternion.Euler(90f, 0, 0), glow);
            }

            // Command Head & Glowing Cross Crest
            Transform head = CreatePrimitive(PrimitiveType.Cube, "TitanHead", torso, new Vector3(0, 0.62f, 0.05f), new Vector3(0.30f, 0.28f, 0.28f), Quaternion.identity, armor).transform;
            CreatePrimitive(PrimitiveType.Cube, "HeadOptics", head, new Vector3(0, 0.02f, 0.48f), new Vector3(0.24f, 0.08f, 0.05f), Quaternion.identity, glow);

            // Mechanical Cross Crest atop King's head
            Transform crossRoot = new GameObject("TitanCrossCrest").transform;
            crossRoot.SetParent(head, false);
            crossRoot.localPosition = new Vector3(0, 0.52f, 0);

            CreatePrimitive(PrimitiveType.Cube, "CrossVertical", crossRoot, Vector3.zero, new Vector3(0.08f, 0.32f, 0.06f), Quaternion.identity, glow);
            CreatePrimitive(PrimitiveType.Cube, "CrossHorizontal", crossRoot, new Vector3(0, 0.05f, 0), new Vector3(0.24f, 0.08f, 0.06f), Quaternion.identity, glow);
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
