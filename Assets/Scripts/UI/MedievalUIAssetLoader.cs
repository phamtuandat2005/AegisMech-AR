using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MechChess.UI
{
    public enum MedievalButtonStyle
    {
        GoldChampion,       // Ornate gold filigree with bright crest / Bắt đầu style
        WoodCarved,         // Dark oak carved plank with bronze studs
        DarkGold,           // Dark bronze/wood with gold inlay
        CrimsonBanner,      // Royal red silk ribbon with gold borders
        ParchmentRibbon,    // Aged parchment scroll ribbon
        BlackBanner,        // Heavy dark iron/leather banner
        RubyRibbon,         // Deep ruby flourish banner
        WoodSmall,          // Compact carved wood square/pill
        GoldBlank,          // Blank gold filigree banner for dynamic text
        HexStone,           // Chamfered stone/parchment hexagon button ("Quay lại")
        HexGold             // Chamfered radiant gold hexagon button ("Tiếp tục")
    }

    public enum MedievalFrameStyle
    {
        GrimoireOpen,       // Open 2-page leather book
        GrimoireClosed,     // Closed leather book with crown seal
        ModalCrowned,       // Baroque framed modal dialog with royal crown top
        VictoryCathedral,   // Epic chess cathedral background with gothic pillars and chess pieces
        ParchmentTall,      // Blank vertical parchment card with filigree
        ParchmentCardMid,   // Medium parchment card
        ParchmentCardBot,   // Compact parchment card
        ParchmentCastle,    // Castle engraving on parchment
        ParchmentSketch,    // Knight & King etching
        Board2D,            // 2D chessboard panel with piece tray
        WorldMap,           // Antique fantasy world map
        SettingsNotebook,   // Notebook with tabs
        BannerTip,          // Parchment alert with bulb icon
        BannerWarning,      // Parchment alert with warning triangle
        BannerBlackGold     // Obsidian leather banner with antique gold rims and swallowtail ends
    }

    public enum MedievalIcon
    {
        Gear,
        Mail,
        Help,
        Info,
        Swords,
        Trophy,
        RoyalLion,
        TrophyShield,
        GoldCoin,
        BlueGem,
        AvatarGold,
        Close,
        DropdownArrow,
        Play,
        Pause,
        Forward,
        StarFilled,
        StarEmpty,
        Hint,
        CrownVictory,
        LaurelLeft,
        LaurelRight,
        DividerOrnate,
        Back,
        Confirm,
        Cancel,
        Ok,
        BadgeChampion,
        BadgeGrandmaster,
        BadgeGoldStar,
        BadgeSilverStar,
        BadgeBronzeStar,
        BadgeCrossedAxes,
        CrestRubyLaurel,
        CrestCrimsonStar,
        CrestObsidianSwords,
        CrestGoldLaurel,
        CrestIronShield
    }

    public enum MedievalBadge
    {
        ChampionCrown,      // Gold Crown with Ruby Red Ribbon
        GrandmasterCrown,   // Gold Crown with Royal Blue Ribbon
        GoldStar,           // Gold Star with Royal Blue Ribbon
        SilverStar,         // Silver Star with Split Blue/Red Ribbon
        BronzeStar,         // Bronze Star with Crimson Ribbon
        CrossedAxes         // Royal Military Medallion with Crossed Axes
    }

    public enum MedievalCrest
    {
        RubyLaurel,         // Grandmaster laurel wreath with crimson star/knight
        CrimsonStar,        // Diamond baroque shield with 3D gold star
        ObsidianSwords,     // Platinum obsidian shield with crossed swords
        GoldLaurel,         // Gold laurel wreath with bronze round shield
        IronShield,         // Bronze/Iron baroque kite shield
        RoyalLion,          // Royal lion crest
        TrophyShield        // Trophy shield crest
    }

    /// <summary>
    /// Loads and manages all Medieval Manuscript / Fantasy Chess textures, sprites,
    /// 9-slicing borders, and procedural fallbacks.
    /// </summary>
    public static class MedievalUIAssetLoader
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        // Default 9-slice borders for common elements (Left, Bottom, Right, Top)
        private static readonly Dictionary<string, Vector4> _sliceBorders = new Dictionary<string, Vector4>
        {
            { "btn_wood_blank_baroque", new Vector4(180, 35, 180, 35) },
            { "btn_parchment_blank_baroque", new Vector4(120, 25, 120, 25) },
            { "btn_blank_parchment_gold", new Vector4(120, 25, 120, 25) },
            { "btn_blank_dark_gold", new Vector4(60, 25, 60, 25) },
            { "btn_blank_crimson_banner", new Vector4(35, 12, 35, 12) },
            { "btn_blank_parchment_ribbon", new Vector4(30, 12, 30, 12) },
            { "banner_gold_ribbon", new Vector4(120, 30, 120, 30) },
            { "btn_blank_black_banner", new Vector4(60, 15, 60, 15) },
            { "btn_blank_ruby_ribbon", new Vector4(30, 12, 30, 12) },
            { "btn_wood_blank_1", new Vector4(180, 35, 180, 35) },
            { "btn_wood_blank_2", new Vector4(120, 25, 120, 25) },
            { "btn_wood_blank_3", new Vector4(180, 35, 180, 35) },
            { "btn_gold_start", Vector4.zero },
            { "btn_wood_mode", Vector4.zero },
            { "btn_wood_shop", Vector4.zero },
            { "btn_wood_collection", Vector4.zero },
            { "btn_wood_settings", Vector4.zero },
            { "btn_wood_quit", Vector4.zero },
            { "btn_wood_close", Vector4.zero },
            { "btn_pause_resume", Vector4.zero },
            { "btn_pause_settings", Vector4.zero },
            { "btn_pause_menu", Vector4.zero },
            { "btn_pause_back", Vector4.zero },
            { "btn_pause_blank", new Vector4(60, 25, 60, 25) },
            { "btn_close_x", new Vector4(30, 30, 30, 30) },
            { "icon_close_cross", new Vector4(30, 30, 30, 30) },
            { "dropdown_parchment_box", new Vector4(40, 22, 95, 22) },
            { "parchment_box_clean", new Vector4(40, 22, 40, 22) },
            { "icon_dropdown_arrow", Vector4.zero },
            { "icon_round_play", Vector4.zero },
            { "icon_round_pause", Vector4.zero },
            { "icon_round_forward", Vector4.zero },
            { "btn_round_play", Vector4.zero },
            { "btn_round_pause", Vector4.zero },
            { "btn_round_forward", Vector4.zero },
            { "icon_star_filled", Vector4.zero },
            { "icon_star_empty", Vector4.zero },
            { "icon_hint_bulb", Vector4.zero },
            { "icon_round_hint", Vector4.zero },
            { "btn_round_hint", Vector4.zero },
            { "crown_victory_gold", Vector4.zero },
            { "laurel_branch_gold_left", Vector4.zero },
            { "laurel_branch_gold_right", Vector4.zero },
            { "divider_ornate_gold", Vector4.zero },
            { "bg_victory_cathedral_clean", Vector4.zero },
            { "bg_victory_cathedral", Vector4.zero },
            { "btn_hex_parchment_stone", new Vector4(45, 20, 45, 20) },
            { "btn_hex_gold_radiant", new Vector4(45, 20, 45, 20) },
            { "row_stat_stripe", new Vector4(20, 6, 20, 6) },
            { "btn_wood_back", new Vector4(25, 10, 25, 10) },
            { "btn_wood_confirm", new Vector4(25, 10, 25, 10) },
            { "btn_wood_cancel", new Vector4(25, 10, 25, 10) },
            { "btn_wood_ok", new Vector4(25, 10, 25, 10) },
            { "scrollbar_track_vertical", new Vector4(4, 10, 4, 10) },
            { "scrollbar_thumb_vertical", new Vector4(4, 16, 4, 16) },
            { "tab_tutorial_gold_active", new Vector4(45, 14, 45, 14) },
            { "tab_tutorial_gold_inactive", new Vector4(45, 14, 45, 14) },
            
            // Frames
            { "frame_mode_card_royal", new Vector4(28, 28, 28, 28) },
            { "frame_mode_card_royal_active", new Vector4(28, 28, 28, 28) },
            { "frame_shop_collection_1520", new Vector4(90, 80, 90, 80) },
            { "frame_modal_crowned", new Vector4(90, 70, 90, 110) },
            { "panel_modal_notice", new Vector4(90, 70, 90, 110) },
            { "frame_parchment_tall", new Vector4(45, 50, 45, 55) },
            { "panel_parchment_tall", new Vector4(45, 50, 45, 55) },
            { "frame_parchment_card_mid", new Vector4(30, 25, 30, 25) },
            { "frame_parchment_card_bot", new Vector4(30, 25, 30, 25) },
            { "frame_parchment_banner_top", new Vector4(30, 20, 30, 20) },
            { "frame_parchment_castle", new Vector4(40, 40, 40, 40) },
            { "frame_parchment_sketch", new Vector4(40, 40, 40, 40) },
            { "ribbon_crimson", new Vector4(40, 10, 40, 10) },
            { "ribbon_parchment", new Vector4(160, 35, 160, 30) },
            { "banner_parchment_scroll", new Vector4(160, 35, 160, 30) },
            { "banner_tip_full", new Vector4(180, 20, 60, 20) },
            { "banner_warning_full", new Vector4(180, 20, 60, 20) },
            { "slider_bar_long", new Vector4(30, 8, 30, 8) },
            { "loading_bar_frame", new Vector4(55, 8, 55, 8) },
            { "loading_tip_banner", new Vector4(180, 20, 60, 20) },
            { "loading_bar_fill", new Vector4(15, 6, 15, 6) },
            { "tab_parchment_gold_active", new Vector4(56, 22, 56, 20) },
            { "tab_parchment_gold_inactive", new Vector4(56, 22, 56, 20) }
        };

        public static Sprite GetSprite(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            if (_spriteCache.TryGetValue(assetName, out Sprite cached) && cached != null)
                return cached;

            string cleanName = assetName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? assetName.Substring(0, assetName.Length - 4)
                : assetName;

            // 1. Try Resources.Load
            string resPath = "Textures/MedievalUI/" + cleanName;
            Sprite sprite = Resources.Load<Sprite>(resPath);

            // 2. Fallback to direct file loading if not yet imported by Unity
            if (sprite == null)
            {
                string filePath = Path.Combine(Application.dataPath, "Resources", "Textures", "MedievalUI", cleanName + ".png");
                if (File.Exists(filePath))
                {
                    try
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(bytes))
                        {
                            tex.wrapMode = TextureWrapMode.Clamp;
                            tex.filterMode = FilterMode.Bilinear;

                            Vector4 border = Vector4.zero;
                            if (_sliceBorders.TryGetValue(assetName, out Vector4 b))
                                border = b;

                            sprite = Sprite.Create(
                                tex,
                                new Rect(0, 0, tex.width, tex.height),
                                new Vector2(0.5f, 0.5f),
                                100.0f,
                                0,
                                SpriteMeshType.FullRect,
                                border
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MedievalUIAssetLoader] Error reading direct file {filePath}: {ex.Message}");
                    }
                }
            }

            // 3. Fallback to procedural sprite if asset missing
            if (sprite == null)
            {
                sprite = CreateProceduralFallback(assetName);
            }

            if (sprite != null)
            {
                _spriteCache[assetName] = sprite;
            }

            return sprite;
        }

        public static Sprite GetButtonSprite(MedievalButtonStyle style)
        {
            switch (style)
            {
                case MedievalButtonStyle.GoldChampion:
                    return GetSprite("btn_parchment_blank_baroque") ?? GetSprite("btn_blank_parchment_gold") ?? GetSprite("btn_gold_start");
                case MedievalButtonStyle.WoodCarved:
                    return GetSprite("btn_wood_blank_baroque") ?? GetSprite("btn_wood_blank_1") ?? GetSprite("btn_blank_dark_gold");
                case MedievalButtonStyle.DarkGold:
                    return GetSprite("btn_blank_dark_gold") ?? GetSprite("btn_wood_blank_baroque");
                case MedievalButtonStyle.CrimsonBanner:
                    return GetSprite("ribbon_crimson") ?? GetSprite("btn_blank_crimson_banner");
                case MedievalButtonStyle.ParchmentRibbon:
                    return GetSprite("btn_parchment_blank_baroque") ?? GetSprite("btn_blank_parchment_ribbon");
                case MedievalButtonStyle.BlackBanner:
                    return GetSprite("banner_gold_ribbon") ?? GetSprite("btn_blank_black_banner");
                case MedievalButtonStyle.RubyRibbon:
                    return GetSprite("btn_blank_ruby_ribbon");
                case MedievalButtonStyle.WoodSmall:
                    return GetSprite("btn_wood_blank_baroque") ?? GetSprite("btn_wood_blank_3");
                case MedievalButtonStyle.HexStone:
                    return GetSprite("btn_pause_back") ?? GetSprite("btn_hex_parchment_stone");
                case MedievalButtonStyle.HexGold:
                    return GetSprite("btn_pause_resume") ?? GetSprite("btn_hex_gold_radiant");
                case MedievalButtonStyle.GoldBlank:
                default:
                    return GetSprite("btn_parchment_blank_baroque") ?? GetSprite("btn_blank_parchment_gold");
            }
        }

        public static Sprite GetFrameSprite(MedievalFrameStyle style)
        {
            switch (style)
            {
                case MedievalFrameStyle.GrimoireOpen:
                    return GetSprite("frame_grimoire_open");
                case MedievalFrameStyle.GrimoireClosed:
                    return GetSprite("frame_grimoire_closed");
                case MedievalFrameStyle.ModalCrowned:
                    return GetSprite("frame_shop_collection_1520") ?? GetSprite("frame_modal_royal_large") ?? GetSprite("panel_modal_notice") ?? GetSprite("frame_modal_crowned");
                case MedievalFrameStyle.VictoryCathedral:
                    return GetSprite("bg_victory_cathedral_clean") ?? GetSprite("bg_victory_cathedral");
                case MedievalFrameStyle.ParchmentTall:
                    return GetSprite("frame_parchment_tall") ?? GetSprite("panel_parchment_tall");
                case MedievalFrameStyle.ParchmentCardMid:
                    return GetSprite("frame_parchment_card_mid");
                case MedievalFrameStyle.ParchmentCardBot:
                    return GetSprite("frame_parchment_card_bot");
                case MedievalFrameStyle.ParchmentCastle:
                    return GetSprite("frame_parchment_castle");
                case MedievalFrameStyle.ParchmentSketch:
                    return GetSprite("frame_parchment_sketch");
                case MedievalFrameStyle.Board2D:
                    return GetSprite("frame_board_2d");
                case MedievalFrameStyle.WorldMap:
                    return GetSprite("map_world_parchment");
                case MedievalFrameStyle.SettingsNotebook:
                    return GetSprite("panel_settings_notebook");
                case MedievalFrameStyle.BannerTip:
                    return GetSprite("banner_tip_full");
                case MedievalFrameStyle.BannerWarning:
                    return GetSprite("banner_warning_full");
                case MedievalFrameStyle.BannerBlackGold:
                    return GetSprite("banner_gold_ribbon") ?? GetSprite("btn_blank_black_banner");
                default:
                    return GetSprite("frame_parchment_card_mid");
            }
        }

        public static Sprite GetChessIcon(string pieceType)
        {
            if (string.IsNullOrEmpty(pieceType)) return null;
            string key = pieceType.ToLowerInvariant();
            if (key.Contains("king")) return GetSprite("chess_king");
            if (key.Contains("queen")) return GetSprite("chess_queen");
            if (key.Contains("rook")) return GetSprite("chess_rook");
            if (key.Contains("bishop")) return GetSprite("chess_bishop");
            if (key.Contains("knight")) return GetSprite("chess_knight");
            if (key.Contains("pawn")) return GetSprite("chess_pawn");
            return GetSprite("chess_pawn");
        }

        public static Sprite GetMechCard(string mechType)
        {
            if (string.IsNullOrEmpty(mechType)) return GetSprite("card_mech_striker");
            string key = mechType.ToLowerInvariant();
            if (key.Contains("titan")) return GetSprite("card_mech_titan");
            if (key.Contains("shadow")) return GetSprite("card_mech_shadow");
            if (key.Contains("valkyrie")) return GetSprite("card_mech_valkyrie");
            return GetSprite("card_mech_striker");
        }

        public static Sprite GetIcon(MedievalIcon icon)
        {
            switch (icon)
            {
                case MedievalIcon.Gear: return GetSprite("icon_round_gear");
                case MedievalIcon.Mail: return GetSprite("icon_mail_envelope") ?? GetSprite("icon_round_mail");
                case MedievalIcon.Help: return GetSprite("icon_round_help");
                case MedievalIcon.Info: return GetSprite("icon_round_info");
                case MedievalIcon.Swords: return GetSprite("btn_blank_parchment_gold"); // crossed swords
                case MedievalIcon.Trophy: return GetSprite("crest_trophy_shield");
                case MedievalIcon.RoyalLion: return GetSprite("crest_royal_lion");
                case MedievalIcon.TrophyShield: return GetSprite("crest_trophy_shield");
                case MedievalIcon.GoldCoin: return GetSprite("icon_gold_coins") ?? GetSprite("counter_gold_capsule");
                case MedievalIcon.BlueGem: return GetSprite("icon_blue_gem") ?? GetSprite("counter_gem_capsule");
                case MedievalIcon.AvatarGold: return GetSprite("avatar_frame_gold") ?? GetSprite("avatar_medallion_gold");
                case MedievalIcon.Back: return GetSprite("btn_wood_back");
                case MedievalIcon.Confirm: return GetSprite("btn_wood_confirm");
                case MedievalIcon.Cancel: return GetSprite("btn_wood_cancel");
                case MedievalIcon.Ok: return GetSprite("btn_wood_ok");
                case MedievalIcon.Close: return GetSprite("btn_wood_close") ?? GetSprite("btn_royal_close_swords") ?? GetSprite("icon_crossed_swords_x") ?? GetSprite("btn_close_x");
                case MedievalIcon.DropdownArrow: return GetSprite("icon_dropdown_arrow");
                case MedievalIcon.Play: return GetSprite("icon_round_play") ?? GetSprite("btn_round_play");
                case MedievalIcon.Pause: return GetSprite("icon_round_pause") ?? GetSprite("btn_round_pause");
                case MedievalIcon.Forward: return GetSprite("icon_round_forward") ?? GetSprite("btn_round_forward");
                case MedievalIcon.StarFilled: return GetSprite("icon_star_filled");
                case MedievalIcon.StarEmpty: return GetSprite("icon_star_empty");
                case MedievalIcon.Hint: return GetSprite("icon_round_hint") ?? GetSprite("icon_hint_bulb");
                case MedievalIcon.CrownVictory: return GetSprite("crown_victory_gold");
                case MedievalIcon.LaurelLeft: return GetSprite("laurel_branch_gold_left");
                case MedievalIcon.LaurelRight: return GetSprite("laurel_branch_gold_right");
                case MedievalIcon.DividerOrnate: return GetSprite("divider_ornate_gold");
                case MedievalIcon.BadgeChampion: return GetBadgeSprite(MedievalBadge.ChampionCrown);
                case MedievalIcon.BadgeGrandmaster: return GetBadgeSprite(MedievalBadge.GrandmasterCrown);
                case MedievalIcon.BadgeGoldStar: return GetBadgeSprite(MedievalBadge.GoldStar);
                case MedievalIcon.BadgeSilverStar: return GetBadgeSprite(MedievalBadge.SilverStar);
                case MedievalIcon.BadgeBronzeStar: return GetBadgeSprite(MedievalBadge.BronzeStar);
                case MedievalIcon.BadgeCrossedAxes: return GetBadgeSprite(MedievalBadge.CrossedAxes);
                case MedievalIcon.CrestRubyLaurel: return GetCrestSprite(MedievalCrest.RubyLaurel);
                case MedievalIcon.CrestCrimsonStar: return GetCrestSprite(MedievalCrest.CrimsonStar);
                case MedievalIcon.CrestObsidianSwords: return GetCrestSprite(MedievalCrest.ObsidianSwords);
                case MedievalIcon.CrestGoldLaurel: return GetCrestSprite(MedievalCrest.GoldLaurel);
                case MedievalIcon.CrestIronShield: return GetCrestSprite(MedievalCrest.IronShield);
                default: return null;
            }
        }

        public static Sprite GetBadgeSprite(MedievalBadge badge)
        {
            switch (badge)
            {
                case MedievalBadge.ChampionCrown: return GetSprite("badge_crown_crimson") ?? GetSprite("medal_rank_champion");
                case MedievalBadge.GrandmasterCrown: return GetSprite("badge_crown_blue") ?? GetSprite("medal_rank_grandmaster");
                case MedievalBadge.GoldStar: return GetSprite("badge_gold_star_blue") ?? GetSprite("medal_rank_gold");
                case MedievalBadge.SilverStar: return GetSprite("badge_silver_star_split") ?? GetSprite("medal_rank_silver");
                case MedievalBadge.BronzeStar: return GetSprite("badge_bronze_star_red") ?? GetSprite("medal_rank_bronze");
                case MedievalBadge.CrossedAxes: return GetSprite("badge_crossed_axes") ?? GetSprite("medal_rank_veteran");
                default: return GetSprite("badge_gold_star_blue");
            }
        }

        public static Sprite GetRankMedalSprite(int rankIndex)
        {
            switch (rankIndex)
            {
                case 0: return GetBadgeSprite(MedievalBadge.ChampionCrown);
                case 1: return GetBadgeSprite(MedievalBadge.GrandmasterCrown);
                case 2: return GetBadgeSprite(MedievalBadge.GoldStar);
                case 3: return GetBadgeSprite(MedievalBadge.SilverStar);
                case 4: return GetBadgeSprite(MedievalBadge.BronzeStar);
                default: return GetBadgeSprite(MedievalBadge.CrossedAxes);
            }
        }

        public static Sprite GetCrestSprite(MedievalCrest crest)
        {
            switch (crest)
            {
                case MedievalCrest.RubyLaurel: return GetSprite("crest_rank_ruby_laurel") ?? GetSprite("crest_tier_grandmaster");
                case MedievalCrest.CrimsonStar: return GetSprite("crest_shield_ruby_star") ?? GetSprite("crest_rank_crimson_star") ?? GetSprite("crest_tier_diamond");
                case MedievalCrest.ObsidianSwords: return GetSprite("crest_rank_obsidian_swords") ?? GetSprite("crest_tier_platinum");
                case MedievalCrest.GoldLaurel: return GetSprite("crest_rank_gold_laurel") ?? GetSprite("crest_tier_gold");
                case MedievalCrest.IronShield: return GetSprite("crest_rank_iron_shield") ?? GetSprite("crest_tier_bronze");
                case MedievalCrest.RoyalLion: return GetSprite("crest_royal_lion");
                case MedievalCrest.TrophyShield: return GetSprite("crest_trophy_shield");
                default: return GetSprite("crest_rank_gold_laurel");
            }
        }

        public static Sprite GetTierCrestSprite(string tierName)
        {
            if (string.IsNullOrEmpty(tierName)) return GetCrestSprite(MedievalCrest.GoldLaurel);
            string t = tierName.ToLowerInvariant();
            if (t.Contains("cao thủ") || t.Contains("huyền thoại") || t.Contains("grandmaster") || t.Contains("legend") || t.Contains("chiến thần"))
                return GetCrestSprite(MedievalCrest.RubyLaurel);
            if (t.Contains("kim cương") || t.Contains("diamond"))
                return GetCrestSprite(MedievalCrest.CrimsonStar);
            if (t.Contains("bạch kim") || t.Contains("platinum"))
                return GetCrestSprite(MedievalCrest.ObsidianSwords);
            if (t.Contains("vàng") || t.Contains("gold"))
                return GetCrestSprite(MedievalCrest.GoldLaurel);
            return GetCrestSprite(MedievalCrest.IronShield);
        }

        private static Sprite CreateProceduralFallback(string name)
        {
            int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color baseColor = new Color(0.92f, 0.85f, 0.72f, 1.0f); // Aged Parchment
            Color borderColor = new Color(0.70f, 0.52f, 0.20f, 1.0f); // Antique Gold

            if (name.Contains("wood") || name.Contains("dark"))
            {
                baseColor = new Color(0.18f, 0.12f, 0.08f, 1.0f); // Dark Oak
                borderColor = new Color(0.45f, 0.32f, 0.18f, 1.0f);
            }
            else if (name.Contains("crimson") || name.Contains("ruby"))
            {
                baseColor = new Color(0.55f, 0.12f, 0.14f, 1.0f); // Royal Crimson
                borderColor = new Color(0.85f, 0.70f, 0.30f, 1.0f);
            }

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < 3 || x >= w - 3 || y < 3 || y >= h - 3);
                    tex.SetPixel(x, y, isBorder ? borderColor : baseColor);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
        }

        public static void ClearCache()
        {
            _spriteCache.Clear();
        }
    }
}
