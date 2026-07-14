using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace WildfrostAccessibility
{
    /// <summary>
    /// Localization system for screen reader strings.
    /// Uses the game's active locale to serve translations.
    /// Falls back: current language -> English -> key name.
    /// </summary>
    public static class Loc
    {
        // language code -> (key -> translation)
        private static readonly Dictionary<string, Dictionary<string, string>> _strings
            = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Initialize and register all screen reader strings.
        /// Called once during mod load.
        /// </summary>
        public static void Initialize()
        {
            RegisterDefaults();
        }

        /// <summary>
        /// Get a localized string for the current game language.
        /// </summary>
        public static string Get(string key)
        {
            string langCode = GetCurrentLanguageCode();

            // Try current language
            if (_strings.TryGetValue(langCode, out var langDict) && langDict.TryGetValue(key, out var text))
                return text;

            // Fall back to English
            if (langCode != "en" && _strings.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enText))
                return enText;

            // Last resort: return key
            DebugLogger.Log(DebugLogger.LogCategory.ScreenReader, $"Missing localization: [{langCode}] {key}");
            return key;
        }

        /// <summary>
        /// Try to get a localized string. Returns false if the key is unknown
        /// in both the current language and the English fallback.
        /// </summary>
        public static bool TryGet(string key, out string text)
        {
            string langCode = GetCurrentLanguageCode();

            if (_strings.TryGetValue(langCode, out var langDict) && langDict.TryGetValue(key, out text))
                return true;

            if (_strings.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out text))
                return true;

            text = null;
            return false;
        }

        /// <summary>
        /// Get a localized string with template parameters.
        /// Use {0}, {1}, etc. as placeholders.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// Add a localized string for a specific language.
        /// </summary>
        public static void Add(string langCode, string key, string value)
        {
            if (!_strings.TryGetValue(langCode, out var langDict))
            {
                langDict = new Dictionary<string, string>();
                _strings[langCode] = langDict;
            }
            langDict[key] = value;
        }

        /// <summary>
        /// Add a string for all languages at once (convenience for language-neutral strings).
        /// </summary>
        public static void AddForAll(string key, string value)
        {
            foreach (var lang in _strings.Keys)
            {
                _strings[lang][key] = value;
            }
            // Also ensure English has it as the ultimate fallback
            Add("en", key, value);
        }

        /// <summary>
        /// Returns the locale code of the game's currently selected language.
        /// </summary>
        public static string GetCurrentLanguageCode()
        {
            try
            {
                Locale locale = LocalizationSettings.SelectedLocale;
                if (locale != null)
                    return locale.Identifier.Code;
            }
            catch
            {
                // Localization system not ready yet
            }
            return "en";
        }

        /// <summary>
        /// Register all default screen reader strings.
        /// Add new languages here as translations are provided.
        /// </summary>
        private static void RegisterDefaults()
        {
            // English
            Add("en", "mod_loaded", "Wildfrost Accessibility loaded. Press F1 for help.");
            Add("en", "mod_unloaded", "Wildfrost Accessibility unloaded.");
            Add("en", "help_text", "Wildfrost Accessibility. F1: this help. F10: toggle debug mode. Arrow keys: navigate. Enter: activate.");
            Add("en", "screen_main_menu", "Main Menu. Use up and down arrows to navigate, Enter to select.");
            Add("en", "tutorial_prompt", "Tutorial: {0}");
            Add("en", "slot_your_side", "Your side");
            Add("en", "slot_enemy_side", "Enemy side");
            Add("en", "slot_row", "Row {0}");
            Add("en", "slot_position", "Slot {0}");
            Add("en", "slot_empty", "Empty");
            Add("en", "slot_occupied", "Occupied");
            Add("en", "building_under_construction", "Under construction");
            Add("en", "building_new_unlock", "New unlock available");

            // German
            Add("de", "mod_loaded", "Wildfrost Barrierefreiheit geladen. F1 fuer Hilfe.");
            Add("de", "mod_unloaded", "Wildfrost Barrierefreiheit deaktiviert.");
            Add("de", "help_text", "Wildfrost Barrierefreiheit. F1: diese Hilfe. F10: Debug-Modus umschalten. Pfeiltasten: navigieren. Enter: auswaehlen.");
            Add("de", "screen_main_menu", "Hauptmenue. Pfeiltasten hoch und runter zum Navigieren, Enter zum Auswaehlen.");
            Add("de", "tutorial_prompt", "Anleitung: {0}");
            Add("de", "slot_your_side", "Deine Seite");
            Add("de", "slot_enemy_side", "Gegnerseite");
            Add("de", "slot_row", "Reihe {0}");
            Add("de", "slot_position", "Platz {0}");
            Add("de", "slot_empty", "Leer");
            Add("de", "slot_occupied", "Belegt");
            Add("de", "building_under_construction", "Im Bau");
            Add("de", "building_new_unlock", "Neue Freischaltung verfuegbar");

            // French
            Add("fr", "mod_loaded", "Accessibilite Wildfrost chargee. F1 pour l'aide.");
            Add("fr", "mod_unloaded", "Accessibilite Wildfrost dechargee.");
            Add("fr", "help_text", "Accessibilite Wildfrost. F1: cette aide. F10: basculer le mode debogage. Fleches: naviguer. Entree: selectionner.");
            Add("fr", "screen_main_menu", "Menu principal. Fleches haut et bas pour naviguer, Entree pour selectionner.");
            Add("fr", "tutorial_prompt", "Tutoriel: {0}");
            Add("fr", "slot_your_side", "Votre cote");
            Add("fr", "slot_enemy_side", "Cote ennemi");
            Add("fr", "slot_row", "Rangee {0}");
            Add("fr", "slot_position", "Emplacement {0}");
            Add("fr", "slot_empty", "Vide");
            Add("fr", "slot_occupied", "Occupe");
            Add("fr", "building_under_construction", "En construction");
            Add("fr", "building_new_unlock", "Nouveau deblocage disponible");

            // Spanish
            Add("es", "mod_loaded", "Accesibilidad de Wildfrost cargada. F1 para ayuda.");
            Add("es", "mod_unloaded", "Accesibilidad de Wildfrost descargada.");
            Add("es", "help_text", "Accesibilidad de Wildfrost. F1: esta ayuda. F10: alternar modo de depuracion. Flechas: navegar. Enter: seleccionar.");
            Add("es", "screen_main_menu", "Menu principal. Flechas arriba y abajo para navegar, Enter para seleccionar.");
            Add("es", "tutorial_prompt", "Tutorial: {0}");
            Add("es", "slot_your_side", "Tu lado");
            Add("es", "slot_enemy_side", "Lado enemigo");
            Add("es", "slot_row", "Fila {0}");
            Add("es", "slot_position", "Casilla {0}");
            Add("es", "slot_empty", "Vacio");
            Add("es", "slot_occupied", "Ocupado");
            Add("es", "building_under_construction", "En construccion");
            Add("es", "building_new_unlock", "Nuevo desbloqueo disponible");

            // Japanese
            Add("ja", "mod_loaded", "Wildfrost アクセシビリティが読み込まれました。F1でヘルプ。");
            Add("ja", "mod_unloaded", "Wildfrost アクセシビリティが無効になりました。");
            Add("ja", "help_text", "Wildfrost アクセシビリティ。F1: ヘルプ。F10: デバッグモード切替。矢印キー: 移動。Enter: 決定。");
            Add("ja", "screen_main_menu", "メインメニュー。上下矢印キーで移動、Enterで選択。");
            Add("ja", "tutorial_prompt", "チュートリアル: {0}");
            Add("ja", "slot_your_side", "味方側");
            Add("ja", "slot_enemy_side", "敵側");
            Add("ja", "slot_row", "{0}列目");
            Add("ja", "slot_position", "{0}番");
            Add("ja", "slot_empty", "空き");
            Add("ja", "slot_occupied", "使用中");

            // Korean
            Add("ko", "mod_loaded", "Wildfrost 접근성 모드 로드됨. F1 도움말.");
            Add("ko", "mod_unloaded", "Wildfrost 접근성 모드 해제됨.");
            Add("ko", "help_text", "Wildfrost 접근성. F1: 도움말. F10: 디버그 모드 전환. 방향키: 탐색. Enter: 선택.");
            Add("ko", "screen_main_menu", "메인 메뉴. 위아래 방향키로 탐색, Enter로 선택.");
            Add("ko", "tutorial_prompt", "튜토리얼: {0}");
            Add("ko", "slot_your_side", "아군 측");
            Add("ko", "slot_enemy_side", "적 측");
            Add("ko", "slot_row", "{0}행");
            Add("ko", "slot_position", "{0}번 칸");
            Add("ko", "slot_empty", "비어있음");
            Add("ko", "slot_occupied", "점유됨");

            // Simplified Chinese
            Add("zh-Hans", "mod_loaded", "Wildfrost 无障碍已加载。按F1获取帮助。");
            Add("zh-Hans", "mod_unloaded", "Wildfrost 无障碍已卸载。");
            Add("zh-Hans", "help_text", "Wildfrost 无障碍。F1: 帮助。F10: 切换调试模式。方向键: 导航。回车: 确认。");
            Add("zh-Hans", "screen_main_menu", "主菜单。上下方向键导航，回车键选择。");
            Add("zh-Hans", "tutorial_prompt", "教程: {0}");
            Add("zh-Hans", "slot_your_side", "己方");
            Add("zh-Hans", "slot_enemy_side", "敌方");
            Add("zh-Hans", "slot_row", "第{0}排");
            Add("zh-Hans", "slot_position", "第{0}格");
            Add("zh-Hans", "slot_empty", "空");
            Add("zh-Hans", "slot_occupied", "已占用");

            // Traditional Chinese
            Add("zh-Hant", "mod_loaded", "Wildfrost 無障礙已載入。按F1取得說明。");
            Add("zh-Hant", "mod_unloaded", "Wildfrost 無障礙已卸載。");
            Add("zh-Hant", "help_text", "Wildfrost 無障礙。F1: 說明。F10: 切換偵錯模式。方向鍵: 導航。Enter: 確認。");
            Add("zh-Hant", "screen_main_menu", "主選單。上下方向鍵導航，Enter鍵選擇。");
            Add("zh-Hant", "tutorial_prompt", "教學: {0}");
            Add("zh-Hant", "slot_your_side", "己方");
            Add("zh-Hant", "slot_enemy_side", "敵方");
            Add("zh-Hant", "slot_row", "第{0}排");
            Add("zh-Hant", "slot_position", "第{0}格");
            Add("zh-Hant", "slot_empty", "空");
            Add("zh-Hant", "slot_occupied", "已佔用");

            // Italian
            Add("it", "mod_loaded", "Accessibilita Wildfrost caricata. F1 per aiuto.");
            Add("it", "mod_unloaded", "Accessibilita Wildfrost disattivata.");
            Add("it", "help_text", "Accessibilita Wildfrost. F1: aiuto. F10: attiva/disattiva debug. Frecce: navigare. Invio: selezionare.");
            Add("it", "screen_main_menu", "Menu principale. Frecce su e giu per navigare, Invio per selezionare.");
            Add("it", "tutorial_prompt", "Tutorial: {0}");
            Add("it", "slot_your_side", "Il tuo lato");
            Add("it", "slot_enemy_side", "Lato nemico");
            Add("it", "slot_row", "Riga {0}");
            Add("it", "slot_position", "Posizione {0}");
            Add("it", "slot_empty", "Vuoto");
            Add("it", "slot_occupied", "Occupato");

            // Portuguese
            Add("pt", "mod_loaded", "Acessibilidade Wildfrost carregada. F1 para ajuda.");
            Add("pt", "mod_unloaded", "Acessibilidade Wildfrost descarregada.");
            Add("pt", "help_text", "Acessibilidade Wildfrost. F1: ajuda. F10: alternar modo de depuracao. Setas: navegar. Enter: selecionar.");
            Add("pt", "screen_main_menu", "Menu principal. Setas para cima e para baixo para navegar, Enter para selecionar.");
            Add("pt", "tutorial_prompt", "Tutorial: {0}");
            Add("pt", "slot_your_side", "Seu lado");
            Add("pt", "slot_enemy_side", "Lado inimigo");
            Add("pt", "slot_row", "Fileira {0}");
            Add("pt", "slot_position", "Posicao {0}");
            Add("pt", "slot_empty", "Vazio");
            Add("pt", "slot_occupied", "Ocupado");

            // Russian
            Add("ru", "mod_loaded", "Доступность Wildfrost загружена. F1 для справки.");
            Add("ru", "mod_unloaded", "Доступность Wildfrost выгружена.");
            Add("ru", "help_text", "Доступность Wildfrost. F1: справка. F10: режим отладки. Стрелки: навигация. Enter: выбрать.");
            Add("ru", "screen_main_menu", "Главное меню. Стрелки вверх и вниз для навигации, Enter для выбора.");
            Add("ru", "tutorial_prompt", "Обучение: {0}");
            Add("ru", "slot_your_side", "Ваша сторона");
            Add("ru", "slot_enemy_side", "Сторона противника");
            Add("ru", "slot_row", "Ряд {0}");
            Add("ru", "slot_position", "Позиция {0}");
            Add("ru", "slot_empty", "Пусто");
            Add("ru", "slot_occupied", "Занято");

            // Polish
            Add("pl", "mod_loaded", "Dostepnosc Wildfrost zaladowana. F1 aby uzyskac pomoc.");
            Add("pl", "mod_unloaded", "Dostepnosc Wildfrost wylaczona.");
            Add("pl", "help_text", "Dostepnosc Wildfrost. F1: pomoc. F10: tryb debugowania. Strzalki: nawigacja. Enter: wybierz.");
            Add("pl", "screen_main_menu", "Menu glowne. Strzalki gora i dol do nawigacji, Enter aby wybrac.");
            Add("pl", "tutorial_prompt", "Samouczek: {0}");
            Add("pl", "slot_your_side", "Twoja strona");
            Add("pl", "slot_enemy_side", "Strona wroga");
            Add("pl", "slot_row", "Rzad {0}");
            Add("pl", "slot_position", "Pozycja {0}");
            Add("pl", "slot_empty", "Puste");
            Add("pl", "slot_occupied", "Zajete");

            // Turkish
            Add("tr", "mod_loaded", "Wildfrost Erisilebilirlik yuklendi. Yardim icin F1.");
            Add("tr", "mod_unloaded", "Wildfrost Erisilebilirlik devre disi.");
            Add("tr", "help_text", "Wildfrost Erisilebilirlik. F1: yardim. F10: hata ayiklama modu. Ok tuslari: gezinme. Enter: sec.");
            Add("tr", "screen_main_menu", "Ana menu. Yukari ve asagi ok tuslariyla gezinin, Enter ile secin.");
            Add("tr", "tutorial_prompt", "Egitim: {0}");
            Add("tr", "slot_your_side", "Sizin taraf");
            Add("tr", "slot_enemy_side", "Dusman tarafi");
            Add("tr", "slot_row", "Sira {0}");
            Add("tr", "slot_position", "Konum {0}");
            Add("tr", "slot_empty", "Bos");
            Add("tr", "slot_occupied", "Dolu");

            // More languages can be added as needed.
            // The game supports many more locales; they will fall back to English until translated.

            RegisterHandlerStrings();
        }

        /// <summary>
        /// Strings for the dedicated screen handlers (Town, ContinueRun, Map, Battle)
        /// and shared item descriptions. English, German, Spanish and French;
        /// other locales fall back to English until translated.
        /// </summary>
        private static void RegisterHandlerStrings()
        {
            // ----- English -----------------------------------------------------

            // Shared stats and items
            Add("en", "stat_health", "{0} health");
            Add("en", "stat_attack", "{0} attack");
            Add("en", "stat_counter", "counter {0}");
            Add("en", "pocket_draw", "Draw pile, {0} cards");
            Add("en", "pocket_draw_one", "Draw pile, 1 card");
            Add("en", "pocket_discard", "Discard pile, {0} cards");
            Add("en", "pocket_discard_one", "Discard pile, 1 card");
            Add("en", "card_count_multiple", "{0}, {1} copies");
            Add("en", "no_item_focused", "Nothing is focused.");
            Add("en", "no_info_available", "No information available.");
            Add("en", "gold_amount", "Gold: {0}.");

            // Scene names for the generic fallback handler
            Add("en", "scene_CharacterSelect", "Character select screen.");
            Add("en", "scene_Cards", "Card collection screen.");
            Add("en", "scene_Mods", "Mods screen.");
            Add("en", "scene_Credits", "Credits.");
            Add("en", "scene_TownUnlocks", "Town unlocks.");
            Add("en", "scene_Event", "Event.");
            Add("en", "scene_BossReward", "Boss reward selection.");
            Add("en", "scene_BattleWin", "Battle won! Victory screen.");
            Add("en", "scene_CampaignEnd", "Journey over.");

            // Victory screen
            Add("en", "battlewin_continue", "Press Enter to continue.");
            Add("en", "battlewin_injuries", "Injured this battle: {0}.");
            Add("en", "battlewin_not_ready", "The victory screen is still appearing, one moment.");
            Add("en", "help_battlewin", "Victory screen after winning a battle. It may list companions injured in the fight. Press Enter to continue the journey.");

            // Character select
            Add("en", "charselect_leaders", "Choose your leader. Arrow keys browse the leaders, Enter selects one.");
            Add("en", "charselect_chosen", "{0} chosen. Press Enter to confirm, or Escape to put the card back.");
            Add("en", "charselect_chosen_generic", "Card chosen. Press Enter to confirm, or Escape to put it back.");
            Add("en", "charselect_cancelled", "Choice cancelled, back to browsing.");
            Add("en", "charselect_pets", "Choose your starting pet. Arrow keys browse, Enter selects.");
            Add("en", "charselect_starting", "Let's go! Starting the journey.");
            Add("en", "inspect_no_confirm", "This panel cannot be confirmed here. Escape puts the card back.");
            Add("en", "help_charselect", "Character selection. Arrow keys browse the choices and Enter selects one. After selecting, Enter confirms and continues, Escape puts the card back.");

            // Main menu
            Add("en", "help_main_menu", "Main menu. Up and down arrows move between buttons, Enter selects. F1 repeats this help, F10 toggles debug mode.");

            // Town
            Add("en", "screen_town", "Town, your base camp.");
            Add("en", "town_hint", "Arrow keys move between buildings, Enter selects, I describes the focused building. F1 for help.");
            Add("en", "gate_continue_tutorial", "Your tutorial journey is in progress. Press Enter to continue it");
            Add("en", "gate_start_tutorial", "Starts your first journey. The game will offer you the tutorial");
            Add("en", "gate_continue_run", "Your journey is in progress. Press Enter to continue it");
            Add("en", "gate_start_run", "Starts a new journey");
            Add("en", "help_town", "Town. Your base between journeys. Buildings unlock new cards and challenges. Arrow keys move between buildings, I reads what a building does, Enter opens it. The Gate starts or continues your journey.");

            // ContinueRun
            Add("en", "screen_continue_run", "Continue journey. You have a journey in progress.");
            Add("en", "continue_missing_data", "This journey uses missing content and cannot be continued.");
            Add("en", "continue_started", "Started on {0}.");
            Add("en", "continue_leader", "Your leader: {0}.");
            Add("en", "continue_deck", "Deck of {0} cards: {1}.");
            Add("en", "continue_hint", "Arrow keys review the cards and buttons. Enter on Let's Go continues the journey.");
            Add("en", "continue_button_desc", "Continues your journey");
            Add("en", "continue_back_desc", "Returns to town");
            Add("en", "help_continue_run", "Continue journey screen. It shows the run in progress: leader, deck, and start date. Enter on the continue button resumes the journey. The back button returns to town. Give up abandons the run.");

            // Campaign map
            Add("en", "screen_map", "Campaign map.");
            Add("en", "map_zone", "Zone: {0}.");
            Add("en", "map_you_are_at", "You are at {0}.");
            Add("en", "map_destinations", "{0} destinations: {1}.");
            Add("en", "map_hint", "Left and right arrows move along the path, Enter travels. M reads the whole map, I reads details, G reads gold.");
            Add("en", "map_node_here", "you are here");
            Add("en", "map_node_enter", "press Enter to enter");
            Add("en", "map_only_location", "This is the only revealed location right now.");
            Add("en", "map_no_controls", "Nothing else on this screen.");
            Add("en", "map_node_cleared", "cleared");
            Add("en", "map_node_available", "available, press Enter to travel here");
            Add("en", "map_node_available_short", "available");
            Add("en", "map_node_ahead", "further ahead");
            Add("en", "map_node_not_reachable", "not reachable");
            Add("en", "map_battle_waves", "{0} waves");
            Add("en", "map_not_ready", "The map is not ready yet.");
            Add("en", "map_overview", "Map overview, {0} known locations.");
            Add("en", "map_hidden_nodes", "{0} more locations not yet revealed");
            Add("en", "map_wave_enemies", "Wave {0}: {1}");
            Add("en", "help_map", "Campaign map. Your journey is a path of locations. Left and right arrows move between locations. Enter travels to an available location. Up and down arrows reach your deck piles and other controls. M reads the whole map, I reads details of the focused location including enemies, G reads your gold.");

            // Map node categories
            Add("en", "node_type_boss", "boss battle");
            Add("en", "node_type_battle", "battle");
            Add("en", "node_type_shop", "shop");
            Add("en", "node_type_gnomeshop", "gnome shop");
            Add("en", "node_type_charm", "charm event");
            Add("en", "node_type_gold", "treasure");
            Add("en", "node_type_item", "item event");
            Add("en", "node_type_companion", "companion event");
            Add("en", "node_type_copyitem", "item copy event");
            Add("en", "node_type_curseitems", "curse event");
            Add("en", "node_type_injuredcompanion", "injured companion");
            Add("en", "node_type_journalpage", "journal page");
            Add("en", "node_type_charmshop", "charm shop");
            Add("en", "node_type_clunkshop", "clunk shop");
            Add("en", "node_type_muncher", "muncher");
            Add("en", "node_type_event", "event");

            // Battle
            Add("en", "screen_battle", "Battle!");
            Add("en", "battle_wave_total", "{0} enemy waves.");
            Add("en", "battle_hand_count", "{0} cards in hand.");
            Add("en", "battle_hint", "Up and down arrows switch between hand, boards, bell and piles. Left and right move within. Enter picks up and places cards. F1 for battle help.");
            Add("en", "battle_your_turn", "Your turn. {0} cards in hand.");
            Add("en", "battle_resolving", "Turn resolving.");
            Add("en", "battle_over", "Battle over.");
            Add("en", "battle_turn", "Turn {0}.");
            Add("en", "battle_bell_rung", "Redraw bell rung. Drawing a new hand.");
            Add("en", "battle_group_empty", "{0} is empty.");
            Add("en", "battle_nothing_to_focus", "Nothing to focus.");
            Add("en", "group_hand", "Hand");
            Add("en", "group_your_board", "Your board");
            Add("en", "group_enemy_board", "Enemy board");
            Add("en", "group_system", "Bell and piles");
            Add("en", "battle_card_picked_up", "{0} picked up.");
            Add("en", "battle_pickup_hint", "Arrow keys choose a target, Enter places it.");
            Add("en", "battle_card_released", "{0} placed.");
            Add("en", "battle_invalid_target", "Not a valid target.");
            Add("en", "battle_cannot_play", "Cannot play that card right now.");
            Add("en", "battle_bell_not_ready", "The redraw bell is not available right now.");
            Add("en", "battle_hand_empty", "Your hand is empty.");
            Add("en", "battle_acts_in", "acts in {0}");
            Add("en", "battle_no_waves", "No wave information.");
            Add("en", "battle_wave_n", "Wave {0}: {1}");
            Add("en", "battle_boss_wave", "boss wave");
            Add("en", "battle_all_waves_spawned", "All waves have spawned.");
            Add("en", "battle_next_wave", "Next wave in {0} turns.");
            Add("en", "battle_bell_charged", "Redraw bell is charged. Ringing it redraws your hand for free.");
            Add("en", "battle_bell_charging", "Redraw bell is ready in {0} turns. Ringing it now will use your turn.");
            Add("en", "battle_bell_counter", "Charged in {0} turns.");
            Add("en", "battle_phase_play", "Your move.");
            Add("en", "battle_phase_other", "Waiting.");
            Add("en", "battle_hit", "{0} hits {1} for {2}.");
            Add("en", "battle_takes_damage", "{0} takes {1} damage.");
            Add("en", "battle_healed", "{0} recovers {1} health.");
            Add("en", "battle_dodged", "{0} dodged.");
            Add("en", "battle_destroyed", "{0} destroyed.");
            Add("en", "battle_status_applied", "{0} {1} applied to {2}.");
            Add("en", "help_battle", "Battle. Up and down arrows switch groups: hand, your board, enemy board, bell and piles. Left and right arrows move within a group. Enter on a hand card picks it up, arrows choose a target, Enter places it. Enter on one of your units on the board picks it up to move it: a free slot moves it, an occupied slot swaps or shoves, the recall zone takes it off the board. Moving and recalling are free actions that do not end your turn. Escape puts a picked-up card back. Playing a card or ringing the bell ends your turn. Readout keys: H hand, B board, W waves, R bell, T turn, G gold, M modifier bells. Press O for the game menu with settings.");
            Add("en", "battle_unit_picked_up", "{0} picked up from the board.");
            Add("en", "battle_move_hint", "Arrow keys choose a destination slot or the recall zone, Enter confirms, Escape cancels.");
            Add("en", "battle_unit_moved", "{0} moved.");
            Add("en", "battle_unit_recalled", "{0} recalled.");
            Add("en", "battle_free_action", "Free action, your turn continues.");
            Add("en", "battle_pickup_cancelled", "{0} put back.");
            Add("en", "battle_cannot_move", "This unit cannot be moved right now.");
            Add("en", "battle_recall_zone", "Recall zone. Drop the unit here to recall it.");
            Add("en", "battle_play_anchor", "Play zone. Press Enter to play the card without a target.");
            Add("en", "battle_trigger_snowed", "{0} is snowed and cannot act.");
            Add("en", "battle_trigger_nullified", "{0}'s action was cancelled.");
            Add("en", "battle_trigger_smackback", "{0} smacks back at {1}!");
            Add("en", "battle_trigger_laststand", "{0} acts with its last stand!");
            Add("en", "battle_trigger_chain", "{0} is set off by {1}.");
            Add("en", "battle_trigger_acts", "{0} acts.");
            Add("en", "battle_kill_combo", "Combo x{0}!");
            Add("en", "battle_gold_dropped", "{0} gold.");
            Add("en", "battle_crown_deploy_one", "One crowned card in your hand deploys before the battle: press Enter on it to place it now.");
            Add("en", "battle_crown_deploy", "{0} crowned cards in your hand deploy before the battle: place them now.");
            Add("en", "card_crowned", "Crowned, deploys at battle start.");
            Add("en", "card_charm_one", "Charm: {0}.");
            Add("en", "card_charms", "{0} charms: {1}.");
            Add("en", "card_token_one", "Token: {0}.");
            Add("en", "card_tokens", "{0} tokens: {1}.");
            Add("en", "counter_frozen", "counter frozen by Snow");
            Add("en", "card_injured_one", "Injured.");
            Add("en", "card_injured", "Injured x{0}.");
            Add("en", "card_mentions", "Mentions {0}");
            Add("en", "upgrade_charm", "charm");
            Add("en", "upgrade_crown", "crown");
            Add("en", "upgrade_token", "token");
            Add("en", "battle_last_stand", "Last Stand! {0} refuses to fall. The battle comes down to a dice roll. Press Enter to roll the dice.");
            Add("en", "battle_last_stand_generic", "Last Stand! The battle comes down to a dice roll. Press Enter to roll the dice.");
            Add("en", "battle_last_stand_rolling", "Rolling the dice.");
            Add("en", "battle_last_stand_won", "You win the last stand!");
            Add("en", "battle_last_stand_lost", "You lose the last stand.");
            Add("en", "battle_companion_injured", "{0} has been injured!");
            Add("en", "crown_holder_empty", "Crown holder, empty. The crown has been taken.");
            Add("en", "battle_bell_name", "Redraw bell");
            Add("en", "battle_wave_bell_name", "Wave bell");
            Add("en", "battle_wave_incoming", "{0} enemies arriving in {1} turns.");
            Add("en", "battle_wave_overflow", "{0} of them will not fit on the enemy board.");
            Add("en", "battle_wave_call_early", "Can be rung to call the wave early.");
            Add("en", "battle_wave_call_reward", "Reward for ringing now: {0} gold.");
            Add("en", "battle_no_modifiers", "No modifier bells active.");
            Add("en", "battle_modifier_bell", "Modifier bell.");
            Add("en", "screen_pause", "Game menu.");
            Add("en", "pause_hint", "Up and down arrows move through the page, Enter activates. Left and right arrows switch tabs, or change the value on a setting. T jumps to the tabs, Escape goes back. Press O to close the menu.");
            Add("en", "pause_no_tabs", "No tabs reachable here. Press Escape to go back.");
            Add("en", "pause_tab_named", "{0}, tab. Press Enter to open.");
            Add("en", "pause_tab", "Tab. Press Enter to open.");
            Add("en", "pause_tab_opened", "{0} opened.");
            Add("en", "pause_closed", "Menu closed.");
            Add("en", "pause_unavailable", "The menu is not available right now.");
            Add("en", "setting_adjust_hint", "Left and right arrows change the value.");
            Add("en", "setting_percent", "{0} percent");
            Add("en", "nav_nothing", "Nothing to focus here.");
            Add("en", "row_not_interactive", "This entry is read-only.");
            Add("en", "pause_lore_page", "Lore page");
            Add("en", "pause_lore_locked", "locked");
            Add("en", "pause_lore_new", "new");
            Add("en", "pause_lore_open_hint", "Press Enter to read.");
            Add("en", "pause_lore_close_hint", "Press Escape to close the page.");
            Add("en", "pause_lore_closed", "Page closed.");
            Add("en", "stat_no_value", "none");
            Add("en", "help_pause", "Game menu. Up and down arrows move through the page items, Enter activates. Left and right arrows move through the tabs, or change the value when on a setting. T jumps to the tab strip. Escape goes back one level, for example out of a settings category. Tab and Shift Tab also step through the page. Press O to close the menu.");

            // Story events (Event scene, cinema bar text)
            Add("en", "event_prompt_action", "Press Enter.");
            Add("en", "event_crack", "Crack {0} of 4.");
            Add("en", "select_blocked", "This cannot be chosen right now.");
            Add("en", "select_blocked_reason", "Not allowed yet. {0}");
            Add("en", "inspect_opened", "Inspecting {0}. Press Escape to close.");
            Add("en", "inspect_closed", "Inspect closed.");
            Add("en", "nothing_to_inspect", "Nothing to inspect here.");
            Add("en", "help_event", "Event screen. A story event on your journey; its title and story are read as they appear. Arrow keys move between items, Enter activates. I inspects the focused card the way right-click does for sighted players, Escape closes the inspect view.");

            // ----- German -------------------------------------------------------

            Add("de", "stat_health", "{0} Leben");
            Add("de", "stat_attack", "{0} Angriff");
            Add("de", "stat_counter", "Zaehler {0}");
            Add("de", "pocket_draw", "Nachziehstapel, {0} Karten");
            Add("de", "pocket_draw_one", "Nachziehstapel, 1 Karte");
            Add("de", "pocket_discard", "Ablagestapel, {0} Karten");
            Add("de", "pocket_discard_one", "Ablagestapel, 1 Karte");
            Add("de", "card_count_multiple", "{0}, {1} mal");
            Add("de", "no_item_focused", "Nichts fokussiert.");
            Add("de", "no_info_available", "Keine Informationen verfuegbar.");
            Add("de", "gold_amount", "Gold: {0}.");
            Add("de", "scene_CharacterSelect", "Charakterauswahl.");
            Add("de", "scene_Cards", "Kartensammlung.");
            Add("de", "scene_Mods", "Mods.");
            Add("de", "scene_Credits", "Mitwirkende.");
            Add("de", "scene_TownUnlocks", "Stadt-Freischaltungen.");
            Add("de", "scene_Event", "Ereignis.");
            Add("de", "scene_BossReward", "Boss-Belohnung.");
            Add("de", "scene_BattleWin", "Kampf gewonnen! Siegesbildschirm.");
            Add("de", "scene_CampaignEnd", "Reise beendet.");
            Add("de", "battlewin_continue", "Druecke Enter, um fortzufahren.");
            Add("de", "battlewin_injuries", "In diesem Kampf verletzt: {0}.");
            Add("de", "battlewin_not_ready", "Der Siegesbildschirm erscheint noch, einen Moment.");
            Add("de", "help_battlewin", "Siegesbildschirm nach einem gewonnenen Kampf. Zeigt gegebenenfalls im Kampf verletzte Gefaehrten. Enter setzt die Reise fort.");
            Add("de", "charselect_leaders", "Waehle deinen Anfuehrer. Pfeiltasten wechseln zwischen den Anfuehrern, Enter waehlt aus.");
            Add("de", "charselect_chosen", "{0} gewaehlt. Enter bestaetigt, Escape legt die Karte zurueck.");
            Add("de", "charselect_chosen_generic", "Karte gewaehlt. Enter bestaetigt, Escape legt sie zurueck.");
            Add("de", "charselect_cancelled", "Auswahl abgebrochen, zurueck zur Uebersicht.");
            Add("de", "charselect_pets", "Waehle dein Start-Haustier. Pfeiltasten wechseln, Enter waehlt aus.");
            Add("de", "charselect_starting", "Los geht's! Die Reise beginnt.");
            Add("de", "inspect_no_confirm", "Dieses Fenster kann hier nicht bestaetigt werden. Escape legt die Karte zurueck.");
            Add("de", "help_charselect", "Charakterauswahl. Pfeiltasten wechseln zwischen den Optionen, Enter waehlt aus. Nach der Auswahl bestaetigt Enter und setzt fort, Escape legt die Karte zurueck.");
            Add("de", "help_main_menu", "Hauptmenue. Pfeiltasten hoch und runter wechseln die Knoepfe, Enter waehlt aus. F1 wiederholt diese Hilfe, F10 schaltet den Debug-Modus um.");
            Add("de", "screen_town", "Stadt, dein Lager.");
            Add("de", "town_hint", "Pfeiltasten wechseln zwischen Gebaeuden, Enter waehlt aus, I beschreibt das fokussierte Gebaeude. F1 fuer Hilfe.");
            Add("de", "gate_continue_tutorial", "Deine Tutorial-Reise laeuft. Enter setzt sie fort");
            Add("de", "gate_start_tutorial", "Startet deine erste Reise. Das Spiel bietet dir das Tutorial an");
            Add("de", "gate_continue_run", "Deine Reise laeuft. Enter setzt sie fort");
            Add("de", "gate_start_run", "Startet eine neue Reise");
            Add("de", "help_town", "Stadt. Dein Lager zwischen den Reisen. Gebaeude schalten neue Karten und Herausforderungen frei. Pfeiltasten wechseln zwischen Gebaeuden, I liest die Funktion vor, Enter oeffnet. Das Tor startet deine Reise oder setzt sie fort.");
            Add("de", "screen_continue_run", "Reise fortsetzen. Du hast eine laufende Reise.");
            Add("de", "continue_missing_data", "Diese Reise nutzt fehlende Inhalte und kann nicht fortgesetzt werden.");
            Add("de", "continue_started", "Gestartet am {0}.");
            Add("de", "continue_leader", "Dein Anfuehrer: {0}.");
            Add("de", "continue_deck", "Deck mit {0} Karten: {1}.");
            Add("de", "continue_hint", "Pfeiltasten gehen durch Karten und Knoepfe. Enter auf Los geht's setzt die Reise fort.");
            Add("de", "continue_button_desc", "Setzt deine Reise fort");
            Add("de", "continue_back_desc", "Zurueck zur Stadt");
            Add("de", "help_continue_run", "Reise-fortsetzen-Bildschirm. Zeigt die laufende Reise: Anfuehrer, Deck und Startdatum. Enter auf dem Fortsetzen-Knopf setzt die Reise fort. Der Zurueck-Knopf fuehrt zur Stadt. Aufgeben verwirft die Reise.");
            Add("de", "screen_map", "Reisekarte.");
            Add("de", "map_zone", "Zone: {0}.");
            Add("de", "map_you_are_at", "Du bist bei {0}.");
            Add("de", "map_destinations", "{0} Ziele: {1}.");
            Add("de", "map_hint", "Pfeiltasten links und rechts bewegen sich auf dem Pfad, Enter reist. M liest die ganze Karte, I liest Details, G liest Gold.");
            Add("de", "map_node_here", "du bist hier");
            Add("de", "map_node_enter", "Enter betritt den Ort");
            Add("de", "map_only_location", "Das ist gerade der einzige aufgedeckte Ort.");
            Add("de", "map_no_controls", "Sonst nichts auf diesem Bildschirm.");
            Add("de", "map_node_cleared", "abgeschlossen");
            Add("de", "map_node_available", "verfuegbar, Enter reist hierher");
            Add("de", "map_node_available_short", "verfuegbar");
            Add("de", "map_node_ahead", "weiter voraus");
            Add("de", "map_node_not_reachable", "nicht erreichbar");
            Add("de", "map_battle_waves", "{0} Wellen");
            Add("de", "map_not_ready", "Die Karte ist noch nicht bereit.");
            Add("de", "map_overview", "Kartenuebersicht, {0} bekannte Orte.");
            Add("de", "map_hidden_nodes", "{0} weitere Orte noch nicht aufgedeckt");
            Add("de", "map_wave_enemies", "Welle {0}: {1}");
            Add("de", "help_map", "Reisekarte. Deine Reise ist ein Pfad aus Orten. Pfeiltasten links und rechts wechseln zwischen Orten. Enter reist zu einem verfuegbaren Ort. Hoch und runter erreichen Kartenstapel und weitere Elemente. M liest die ganze Karte, I liest Details samt Gegnern, G liest dein Gold.");
            Add("de", "node_type_boss", "Bosskampf");
            Add("de", "node_type_battle", "Kampf");
            Add("de", "node_type_shop", "Laden");
            Add("de", "node_type_gnomeshop", "Gnomladen");
            Add("de", "node_type_charm", "Talisman-Ereignis");
            Add("de", "node_type_gold", "Schatz");
            Add("de", "node_type_item", "Gegenstand-Ereignis");
            Add("de", "node_type_companion", "Gefaehrten-Ereignis");
            Add("de", "node_type_copyitem", "Gegenstand-Kopier-Ereignis");
            Add("de", "node_type_curseitems", "Fluch-Ereignis");
            Add("de", "node_type_injuredcompanion", "verletzter Gefaehrte");
            Add("de", "node_type_journalpage", "Journalseite");
            Add("de", "node_type_charmshop", "Talisman-Laden");
            Add("de", "node_type_clunkshop", "Klunker-Laden");
            Add("de", "node_type_muncher", "Muncher");
            Add("de", "node_type_event", "Ereignis");
            Add("de", "screen_battle", "Kampf!");
            Add("de", "battle_wave_total", "{0} Gegnerwellen.");
            Add("de", "battle_hand_count", "{0} Karten auf der Hand.");
            Add("de", "battle_hint", "Hoch und runter wechseln zwischen Hand, Feldern, Glocke und Stapeln. Links und rechts bewegen sich innerhalb. Enter nimmt Karten auf und legt sie ab. F1 fuer Kampfhilfe.");
            Add("de", "battle_your_turn", "Dein Zug. {0} Karten auf der Hand.");
            Add("de", "battle_resolving", "Zug wird ausgefuehrt.");
            Add("de", "battle_over", "Kampf vorbei.");
            Add("de", "battle_turn", "Runde {0}.");
            Add("de", "battle_bell_rung", "Nachziehglocke gelaeutet. Neue Hand wird gezogen.");
            Add("de", "battle_group_empty", "{0} ist leer.");
            Add("de", "battle_nothing_to_focus", "Nichts zu fokussieren.");
            Add("de", "group_hand", "Hand");
            Add("de", "group_your_board", "Dein Feld");
            Add("de", "group_enemy_board", "Gegnerfeld");
            Add("de", "group_system", "Glocke und Stapel");
            Add("de", "battle_card_picked_up", "{0} aufgenommen.");
            Add("de", "battle_pickup_hint", "Pfeiltasten waehlen ein Ziel, Enter legt ab.");
            Add("de", "battle_card_released", "{0} abgelegt.");
            Add("de", "battle_invalid_target", "Kein gueltiges Ziel.");
            Add("de", "battle_cannot_play", "Diese Karte kann gerade nicht gespielt werden.");
            Add("de", "battle_bell_not_ready", "Die Nachziehglocke ist gerade nicht verfuegbar.");
            Add("de", "battle_hand_empty", "Deine Hand ist leer.");
            Add("de", "battle_acts_in", "handelt in {0}");
            Add("de", "battle_no_waves", "Keine Welleninformationen.");
            Add("de", "battle_wave_n", "Welle {0}: {1}");
            Add("de", "battle_boss_wave", "Bosswelle");
            Add("de", "battle_all_waves_spawned", "Alle Wellen sind erschienen.");
            Add("de", "battle_next_wave", "Naechste Welle in {0} Runden.");
            Add("de", "battle_bell_charged", "Nachziehglocke ist aufgeladen. Laeuten zieht kostenlos eine neue Hand.");
            Add("de", "battle_bell_charging", "Nachziehglocke ist in {0} Runden bereit. Jetzt laeuten verbraucht deinen Zug.");
            Add("de", "battle_bell_counter", "Aufgeladen in {0} Runden.");
            Add("de", "battle_phase_play", "Du bist dran.");
            Add("de", "battle_phase_other", "Warten.");
            Add("de", "battle_hit", "{0} trifft {1} fuer {2}.");
            Add("de", "battle_takes_damage", "{0} erleidet {1} Schaden.");
            Add("de", "battle_healed", "{0} heilt {1} Leben.");
            Add("de", "battle_dodged", "{0} ist ausgewichen.");
            Add("de", "battle_destroyed", "{0} zerstoert.");
            Add("de", "battle_status_applied", "{0} {1} auf {2} angewendet.");
            Add("de", "help_battle", "Kampf. Hoch und runter wechseln die Gruppen: Hand, dein Feld, Gegnerfeld, Glocke und Stapel. Links und rechts bewegen sich innerhalb einer Gruppe. Enter auf einer Handkarte nimmt sie auf, Pfeiltasten waehlen ein Ziel, Enter legt ab. Enter auf einer deiner Einheiten auf dem Feld nimmt sie zum Bewegen auf: ein freier Platz bewegt sie, ein besetzter Platz tauscht oder schiebt, die Rueckrufzone nimmt sie vom Feld. Bewegen und Zurueckrufen sind freie Aktionen und beenden deinen Zug nicht. Escape legt eine aufgenommene Karte zurueck. Eine Karte spielen oder die Glocke laeuten beendet deinen Zug. Vorlesetasten: H Hand, B Feld, W Wellen, R Glocke, T Runde, G Gold, M Modifikator-Glocken. O oeffnet das Spielmenue mit Einstellungen.");
            Add("de", "battle_unit_picked_up", "{0} vom Feld aufgenommen.");
            Add("de", "battle_move_hint", "Pfeiltasten waehlen einen Zielplatz oder die Rueckrufzone, Enter bestaetigt, Escape bricht ab.");
            Add("de", "battle_unit_moved", "{0} bewegt.");
            Add("de", "battle_unit_recalled", "{0} zurueckgerufen.");
            Add("de", "battle_free_action", "Freie Aktion, dein Zug geht weiter.");
            Add("de", "battle_pickup_cancelled", "{0} zurueckgelegt.");
            Add("de", "battle_cannot_move", "Diese Einheit kann gerade nicht bewegt werden.");
            Add("de", "battle_recall_zone", "Rueckrufzone. Hier ablegen, um die Einheit zurueckzurufen.");
            Add("de", "battle_play_anchor", "Spielzone. Enter spielt die Karte ohne Ziel.");
            Add("de", "battle_trigger_snowed", "{0} ist eingeschneit und kann nicht handeln.");
            Add("de", "battle_trigger_nullified", "Die Aktion von {0} wurde verhindert.");
            Add("de", "battle_trigger_smackback", "{0} schlaegt gegen {1} zurueck!");
            Add("de", "battle_trigger_laststand", "{0} handelt mit letzter Kraft!");
            Add("de", "battle_trigger_chain", "{0} wird von {1} ausgeloest.");
            Add("de", "battle_trigger_acts", "{0} handelt.");
            Add("de", "battle_kill_combo", "Combo x{0}!");
            Add("de", "battle_gold_dropped", "{0} Gold.");
            Add("de", "battle_crown_deploy_one", "Eine gekroente Karte in deiner Hand wird vor dem Kampf eingesetzt: Enter darauf, um sie jetzt zu platzieren.");
            Add("de", "battle_crown_deploy", "{0} gekroente Karten in deiner Hand werden vor dem Kampf eingesetzt: platziere sie jetzt.");
            Add("de", "card_crowned", "Gekroent, wird bei Kampfbeginn eingesetzt.");
            Add("de", "card_charm_one", "Talisman: {0}.");
            Add("de", "card_charms", "{0} Talismane: {1}.");
            Add("de", "card_token_one", "Marke: {0}.");
            Add("de", "card_tokens", "{0} Marken: {1}.");
            Add("de", "counter_frozen", "Zaehler durch Schnee eingefroren");
            Add("de", "card_injured_one", "Verletzt.");
            Add("de", "card_injured", "Verletzt x{0}.");
            Add("de", "card_mentions", "Erwaehnt {0}");
            Add("de", "upgrade_charm", "Talisman");
            Add("de", "upgrade_crown", "Krone");
            Add("de", "upgrade_token", "Marke");
            Add("de", "battle_last_stand", "Letztes Gefecht! {0} weigert sich zu fallen. Der Kampf entscheidet sich per Wuerfelwurf. Enter wuerfelt.");
            Add("de", "battle_last_stand_generic", "Letztes Gefecht! Der Kampf entscheidet sich per Wuerfelwurf. Enter wuerfelt.");
            Add("de", "battle_last_stand_rolling", "Die Wuerfel rollen.");
            Add("de", "battle_last_stand_won", "Du gewinnst das letzte Gefecht!");
            Add("de", "battle_last_stand_lost", "Du verlierst das letzte Gefecht.");
            Add("de", "battle_companion_injured", "{0} wurde verletzt!");
            Add("de", "crown_holder_empty", "Kronenhalter, leer. Die Krone wurde genommen.");
            Add("de", "battle_bell_name", "Nachziehglocke");
            Add("de", "battle_wave_bell_name", "Wellenglocke");
            Add("de", "battle_wave_incoming", "{0} Gegner treffen in {1} Zuegen ein.");
            Add("de", "battle_wave_overflow", "{0} davon passen nicht auf das gegnerische Feld.");
            Add("de", "battle_wave_call_early", "Kann gelaeutet werden, um die Welle frueher zu rufen.");
            Add("de", "battle_wave_call_reward", "Belohnung fuer sofortiges Laeuten: {0} Gold.");
            Add("de", "battle_no_modifiers", "Keine Modifikator-Glocken aktiv.");
            Add("de", "battle_modifier_bell", "Modifikator-Glocke.");
            Add("de", "screen_pause", "Spielmenue.");
            Add("de", "pause_hint", "Hoch und runter bewegen sich durch die Seite, Enter aktiviert. Links und rechts wechseln die Tabs oder aendern den Wert einer Einstellung. T springt zu den Tabs, Escape geht zurueck. O schliesst das Menue.");
            Add("de", "pause_no_tabs", "Hier sind keine Tabs erreichbar. Escape geht zurueck.");
            Add("de", "pause_tab_named", "{0}, Tab. Enter zum Oeffnen.");
            Add("de", "pause_tab", "Tab. Enter zum Oeffnen.");
            Add("de", "pause_tab_opened", "{0} geoeffnet.");
            Add("de", "pause_closed", "Menue geschlossen.");
            Add("de", "pause_unavailable", "Das Menue ist gerade nicht verfuegbar.");
            Add("de", "setting_adjust_hint", "Links und rechts aendern den Wert.");
            Add("de", "setting_percent", "{0} Prozent");
            Add("de", "nav_nothing", "Hier gibt es nichts zu fokussieren.");
            Add("de", "row_not_interactive", "Dieser Eintrag ist nur lesbar.");
            Add("de", "pause_lore_page", "Geschichtenseite");
            Add("de", "pause_lore_locked", "gesperrt");
            Add("de", "pause_lore_new", "neu");
            Add("de", "pause_lore_open_hint", "Enter zum Vorlesen.");
            Add("de", "pause_lore_close_hint", "Escape schliesst die Seite.");
            Add("de", "pause_lore_closed", "Seite geschlossen.");
            Add("de", "stat_no_value", "keine");
            Add("de", "help_pause", "Spielmenue. Hoch und runter bewegen sich durch die Seitenelemente, Enter aktiviert. Links und rechts bewegen sich durch die Tabs oder aendern den Wert einer Einstellung. T springt zur Tab-Leiste. Escape geht eine Ebene zurueck, zum Beispiel aus einer Einstellungskategorie. Tab und Umschalt Tab gehen ebenfalls durch die Seite. O schliesst das Menue.");
            Add("de", "event_prompt_action", "Enter druecken.");
            Add("de", "event_crack", "Riss {0} von 4.");
            Add("de", "select_blocked", "Das kann gerade nicht gewaehlt werden.");
            Add("de", "select_blocked_reason", "Noch nicht moeglich. {0}");
            Add("de", "inspect_opened", "Untersuche {0}. Escape schliesst.");
            Add("de", "inspect_closed", "Untersuchung geschlossen.");
            Add("de", "nothing_to_inspect", "Hier gibt es nichts zu untersuchen.");
            Add("de", "help_event", "Ereignis-Bildschirm. Ein Story-Ereignis auf deiner Reise; Titel und Text werden vorgelesen, sobald sie erscheinen. Pfeiltasten wechseln zwischen Elementen, Enter aktiviert. I untersucht die fokussierte Karte, so wie Rechtsklick fuer sehende Spieler, Escape schliesst die Untersuchung.");

            // ----- Spanish ------------------------------------------------------

            Add("es", "stat_health", "{0} de vida");
            Add("es", "stat_attack", "{0} de ataque");
            Add("es", "stat_counter", "contador {0}");
            Add("es", "pocket_draw", "Mazo de robo, {0} cartas");
            Add("es", "pocket_draw_one", "Mazo de robo, 1 carta");
            Add("es", "pocket_discard", "Pila de descarte, {0} cartas");
            Add("es", "pocket_discard_one", "Pila de descarte, 1 carta");
            Add("es", "card_count_multiple", "{0}, {1} copias");
            Add("es", "no_item_focused", "Nada seleccionado.");
            Add("es", "no_info_available", "No hay informacion disponible.");
            Add("es", "gold_amount", "Oro: {0}.");
            Add("es", "scene_CharacterSelect", "Seleccion de personaje.");
            Add("es", "scene_Cards", "Coleccion de cartas.");
            Add("es", "scene_Mods", "Mods.");
            Add("es", "scene_Credits", "Creditos.");
            Add("es", "scene_TownUnlocks", "Desbloqueos del pueblo.");
            Add("es", "scene_Event", "Evento.");
            Add("es", "scene_BossReward", "Recompensa de jefe.");
            Add("es", "scene_BattleWin", "Batalla ganada! Pantalla de victoria.");
            Add("es", "scene_CampaignEnd", "Fin del viaje.");
            Add("es", "battlewin_continue", "Pulsa Enter para continuar.");
            Add("es", "battlewin_injuries", "Heridos en esta batalla: {0}.");
            Add("es", "battlewin_not_ready", "La pantalla de victoria todavia esta apareciendo, un momento.");
            Add("es", "help_battlewin", "Pantalla de victoria tras ganar una batalla. Puede mostrar companeros heridos en el combate. Enter continua el viaje.");
            Add("es", "charselect_leaders", "Elige a tu lider. Las flechas cambian de lider, Enter selecciona.");
            Add("es", "charselect_chosen", "{0} elegido. Enter confirma, Escape devuelve la carta.");
            Add("es", "charselect_chosen_generic", "Carta elegida. Enter confirma, Escape la devuelve.");
            Add("es", "charselect_cancelled", "Eleccion cancelada, de vuelta a la lista.");
            Add("es", "charselect_pets", "Elige tu mascota inicial. Las flechas cambian, Enter selecciona.");
            Add("es", "charselect_starting", "Vamos! Comienza el viaje.");
            Add("es", "inspect_no_confirm", "Este panel no se puede confirmar aqui. Escape devuelve la carta.");
            Add("es", "help_charselect", "Seleccion de personaje. Las flechas cambian entre las opciones y Enter selecciona. Tras seleccionar, Enter confirma y continua, Escape devuelve la carta.");
            Add("es", "help_main_menu", "Menu principal. Flechas arriba y abajo cambian de boton, Enter selecciona. F1 repite esta ayuda, F10 alterna el modo de depuracion.");
            Add("es", "screen_town", "Pueblo, tu campamento base.");
            Add("es", "town_hint", "Las flechas cambian de edificio, Enter selecciona, I describe el edificio seleccionado. F1 para ayuda.");
            Add("es", "gate_continue_tutorial", "Tu viaje de tutorial esta en curso. Pulsa Enter para continuarlo");
            Add("es", "gate_start_tutorial", "Comienza tu primer viaje. El juego te ofrecera el tutorial");
            Add("es", "gate_continue_run", "Tu viaje esta en curso. Pulsa Enter para continuarlo");
            Add("es", "gate_start_run", "Comienza un nuevo viaje");
            Add("es", "help_town", "Pueblo. Tu base entre viajes. Los edificios desbloquean cartas y desafios. Las flechas cambian de edificio, I lee su funcion, Enter lo abre. La Puerta comienza o continua tu viaje.");
            Add("es", "screen_continue_run", "Continuar viaje. Tienes un viaje en curso.");
            Add("es", "continue_missing_data", "Este viaje usa contenido que falta y no puede continuarse.");
            Add("es", "continue_started", "Comenzado el {0}.");
            Add("es", "continue_leader", "Tu lider: {0}.");
            Add("es", "continue_deck", "Mazo de {0} cartas: {1}.");
            Add("es", "continue_hint", "Las flechas repasan cartas y botones. Enter en Vamos continua el viaje.");
            Add("es", "continue_button_desc", "Continua tu viaje");
            Add("es", "continue_back_desc", "Vuelve al pueblo");
            Add("es", "help_continue_run", "Pantalla de continuar viaje. Muestra el viaje en curso: lider, mazo y fecha de inicio. Enter en el boton de continuar reanuda el viaje. El boton de volver regresa al pueblo. Rendirse abandona el viaje.");
            Add("es", "screen_map", "Mapa del viaje.");
            Add("es", "map_zone", "Zona: {0}.");
            Add("es", "map_you_are_at", "Estas en {0}.");
            Add("es", "map_destinations", "{0} destinos: {1}.");
            Add("es", "map_hint", "Flechas izquierda y derecha recorren el camino, Enter viaja. M lee todo el mapa, I lee detalles, G lee el oro.");
            Add("es", "map_node_here", "estas aqui");
            Add("es", "map_node_enter", "Enter para entrar");
            Add("es", "map_only_location", "Este es el unico lugar revelado por ahora.");
            Add("es", "map_no_controls", "No hay nada mas en esta pantalla.");
            Add("es", "map_node_cleared", "completado");
            Add("es", "map_node_available", "disponible, pulsa Enter para viajar aqui");
            Add("es", "map_node_available_short", "disponible");
            Add("es", "map_node_ahead", "mas adelante");
            Add("es", "map_node_not_reachable", "no accesible");
            Add("es", "map_battle_waves", "{0} oleadas");
            Add("es", "map_not_ready", "El mapa aun no esta listo.");
            Add("es", "map_overview", "Resumen del mapa, {0} lugares conocidos.");
            Add("es", "map_hidden_nodes", "{0} lugares mas sin revelar");
            Add("es", "map_wave_enemies", "Oleada {0}: {1}");
            Add("es", "help_map", "Mapa del viaje. Tu viaje es un camino de lugares. Flechas izquierda y derecha cambian de lugar. Enter viaja a un lugar disponible. Arriba y abajo alcanzan los mazos y otros controles. M lee todo el mapa, I lee detalles del lugar seleccionado incluidos enemigos, G lee tu oro.");
            Add("es", "node_type_boss", "batalla de jefe");
            Add("es", "node_type_battle", "batalla");
            Add("es", "node_type_shop", "tienda");
            Add("es", "node_type_gnomeshop", "tienda de gnomos");
            Add("es", "node_type_charm", "evento de amuleto");
            Add("es", "node_type_gold", "tesoro");
            Add("es", "node_type_item", "evento de objeto");
            Add("es", "node_type_companion", "evento de companero");
            Add("es", "node_type_copyitem", "evento de copia de objeto");
            Add("es", "node_type_curseitems", "evento de maldicion");
            Add("es", "node_type_injuredcompanion", "companero herido");
            Add("es", "node_type_journalpage", "pagina del diario");
            Add("es", "node_type_charmshop", "tienda de amuletos");
            Add("es", "node_type_clunkshop", "tienda de trastos");
            Add("es", "node_type_muncher", "muncher");
            Add("es", "node_type_event", "evento");
            Add("es", "screen_battle", "Batalla!");
            Add("es", "battle_wave_total", "{0} oleadas enemigas.");
            Add("es", "battle_hand_count", "{0} cartas en mano.");
            Add("es", "battle_hint", "Arriba y abajo cambian entre mano, tableros, campana y pilas. Izquierda y derecha se mueven dentro. Enter coge y coloca cartas. F1 para ayuda de batalla.");
            Add("es", "battle_your_turn", "Tu turno. {0} cartas en mano.");
            Add("es", "battle_resolving", "Resolviendo el turno.");
            Add("es", "battle_over", "Batalla terminada.");
            Add("es", "battle_turn", "Turno {0}.");
            Add("es", "battle_bell_rung", "Campana de robo tocada. Robando una mano nueva.");
            Add("es", "battle_group_empty", "{0} esta vacio.");
            Add("es", "battle_nothing_to_focus", "Nada que seleccionar.");
            Add("es", "group_hand", "Mano");
            Add("es", "group_your_board", "Tu tablero");
            Add("es", "group_enemy_board", "Tablero enemigo");
            Add("es", "group_system", "Campana y pilas");
            Add("es", "battle_card_picked_up", "{0} en mano.");
            Add("es", "battle_pickup_hint", "Las flechas eligen objetivo, Enter la coloca.");
            Add("es", "battle_card_released", "{0} colocada.");
            Add("es", "battle_invalid_target", "Objetivo no valido.");
            Add("es", "battle_cannot_play", "No se puede jugar esa carta ahora.");
            Add("es", "battle_bell_not_ready", "La campana de robo no esta disponible ahora.");
            Add("es", "battle_hand_empty", "Tu mano esta vacia.");
            Add("es", "battle_acts_in", "actua en {0}");
            Add("es", "battle_no_waves", "Sin informacion de oleadas.");
            Add("es", "battle_wave_n", "Oleada {0}: {1}");
            Add("es", "battle_boss_wave", "oleada de jefe");
            Add("es", "battle_all_waves_spawned", "Todas las oleadas han aparecido.");
            Add("es", "battle_next_wave", "Proxima oleada en {0} turnos.");
            Add("es", "battle_bell_charged", "La campana de robo esta cargada. Tocarla roba una mano nueva gratis.");
            Add("es", "battle_bell_charging", "La campana de robo estara lista en {0} turnos. Tocarla ahora gasta tu turno.");
            Add("es", "battle_bell_counter", "Se carga en {0} turnos.");
            Add("es", "battle_phase_play", "Te toca.");
            Add("es", "battle_phase_other", "Esperando.");
            Add("es", "battle_hit", "{0} golpea a {1} por {2}.");
            Add("es", "battle_takes_damage", "{0} recibe {1} de dano.");
            Add("es", "battle_healed", "{0} recupera {1} de vida.");
            Add("es", "battle_dodged", "{0} esquivo el golpe.");
            Add("es", "battle_destroyed", "{0} destruido.");
            Add("es", "battle_status_applied", "{0} de {1} aplicado a {2}.");
            Add("es", "help_battle", "Batalla. Arriba y abajo cambian de grupo: mano, tu tablero, tablero enemigo, campana y pilas. Izquierda y derecha se mueven dentro del grupo. Enter en una carta de la mano la coge, las flechas eligen objetivo, Enter la coloca. Enter en una de tus unidades del tablero la coge para moverla: una casilla libre la mueve, una ocupada intercambia o empuja, la zona de retirada la saca del tablero. Mover y retirar son acciones gratuitas que no terminan tu turno. Escape devuelve la carta cogida. Jugar una carta o tocar la campana termina tu turno. Teclas de lectura: H mano, B tablero, W oleadas, R campana, T turno, G oro, M campanas de modificador. Pulsa O para el menu del juego con los ajustes.");
            Add("es", "battle_unit_picked_up", "{0} levantada del tablero.");
            Add("es", "battle_move_hint", "Las flechas eligen una casilla de destino o la zona de retirada, Enter confirma, Escape cancela.");
            Add("es", "battle_unit_moved", "{0} movida.");
            Add("es", "battle_unit_recalled", "{0} retirada.");
            Add("es", "battle_free_action", "Accion gratuita, tu turno continua.");
            Add("es", "battle_pickup_cancelled", "{0} devuelta.");
            Add("es", "battle_cannot_move", "Esta unidad no se puede mover ahora.");
            Add("es", "battle_recall_zone", "Zona de retirada. Suelta aqui para retirar la unidad.");
            Add("es", "battle_play_anchor", "Zona de juego. Enter juega la carta sin objetivo.");
            Add("es", "battle_trigger_snowed", "{0} esta cubierta de nieve y no puede actuar.");
            Add("es", "battle_trigger_nullified", "La accion de {0} fue anulada.");
            Add("es", "battle_trigger_smackback", "{0} contraataca a {1}!");
            Add("es", "battle_trigger_laststand", "{0} actua con su ultimo aliento!");
            Add("es", "battle_trigger_chain", "{0} es activada por {1}.");
            Add("es", "battle_trigger_acts", "{0} actua.");
            Add("es", "battle_kill_combo", "Combo x{0}!");
            Add("es", "battle_gold_dropped", "{0} de oro.");
            Add("es", "battle_crown_deploy_one", "Una carta coronada en tu mano se despliega antes de la batalla: pulsa Enter sobre ella para colocarla ahora.");
            Add("es", "battle_crown_deploy", "{0} cartas coronadas en tu mano se despliegan antes de la batalla: colocalas ahora.");
            Add("es", "card_crowned", "Coronada, se despliega al inicio de la batalla.");
            Add("es", "card_charm_one", "Amuleto: {0}.");
            Add("es", "card_charms", "{0} amuletos: {1}.");
            Add("es", "card_token_one", "Ficha: {0}.");
            Add("es", "card_tokens", "{0} fichas: {1}.");
            Add("es", "counter_frozen", "contador congelado por la nieve");
            Add("es", "card_injured_one", "Herido.");
            Add("es", "card_injured", "Herido x{0}.");
            Add("es", "card_mentions", "Menciona a {0}");
            Add("es", "upgrade_charm", "amuleto");
            Add("es", "upgrade_crown", "corona");
            Add("es", "upgrade_token", "ficha");
            Add("es", "battle_last_stand", "Ultimo aliento! {0} se niega a caer. La batalla se decide con una tirada de dados. Pulsa Enter para tirar los dados.");
            Add("es", "battle_last_stand_generic", "Ultimo aliento! La batalla se decide con una tirada de dados. Pulsa Enter para tirar los dados.");
            Add("es", "battle_last_stand_rolling", "Tirando los dados.");
            Add("es", "battle_last_stand_won", "Ganas el ultimo aliento!");
            Add("es", "battle_last_stand_lost", "Pierdes el ultimo aliento.");
            Add("es", "battle_companion_injured", "{0} ha sido herido!");
            Add("es", "crown_holder_empty", "Soporte de corona, vacio. La corona ya fue tomada.");
            Add("es", "battle_bell_name", "Campana de robo");
            Add("es", "battle_wave_bell_name", "Campana de oleadas");
            Add("es", "battle_wave_incoming", "{0} enemigos llegan en {1} turnos.");
            Add("es", "battle_wave_overflow", "{0} de ellos no cabran en el tablero enemigo.");
            Add("es", "battle_wave_call_early", "Puede tocarse para llamar la oleada antes.");
            Add("es", "battle_wave_call_reward", "Recompensa por tocarla ahora: {0} de oro.");
            Add("es", "battle_no_modifiers", "No hay campanas de modificador activas.");
            Add("es", "battle_modifier_bell", "Campana de modificador.");
            Add("es", "screen_pause", "Menu del juego.");
            Add("es", "pause_hint", "Arriba y abajo se mueven por la pagina, Enter activa. Izquierda y derecha cambian de pestana, o cambian el valor de un ajuste. T salta a las pestanas, Escape vuelve atras. Pulsa O para cerrar el menu.");
            Add("es", "pause_no_tabs", "No hay pestanas accesibles aqui. Escape vuelve atras.");
            Add("es", "pause_tab_named", "{0}, pestana. Enter para abrir.");
            Add("es", "pause_tab", "Pestana. Enter para abrir.");
            Add("es", "pause_tab_opened", "{0} abierta.");
            Add("es", "pause_closed", "Menu cerrado.");
            Add("es", "pause_unavailable", "El menu no esta disponible ahora.");
            Add("es", "setting_adjust_hint", "Izquierda y derecha cambian el valor.");
            Add("es", "setting_percent", "{0} por ciento");
            Add("es", "nav_nothing", "No hay nada que enfocar aqui.");
            Add("es", "row_not_interactive", "Esta entrada es de solo lectura.");
            Add("es", "pause_lore_page", "Pagina de historia");
            Add("es", "pause_lore_locked", "bloqueada");
            Add("es", "pause_lore_new", "nueva");
            Add("es", "pause_lore_open_hint", "Enter para leer.");
            Add("es", "pause_lore_close_hint", "Escape cierra la pagina.");
            Add("es", "pause_lore_closed", "Pagina cerrada.");
            Add("es", "stat_no_value", "ninguno");
            Add("es", "help_pause", "Menu del juego. Arriba y abajo se mueven por los elementos de la pagina, Enter activa. Izquierda y derecha se mueven por las pestanas, o cambian el valor de un ajuste. T salta a las pestanas. Escape vuelve un nivel atras, por ejemplo fuera de una categoria de ajustes. Tab y Mayus Tab tambien recorren la pagina. Pulsa O para cerrar el menu.");
            Add("es", "event_prompt_action", "Pulsa Enter.");
            Add("es", "event_crack", "Grieta {0} de 4.");
            Add("es", "select_blocked", "Esto no se puede elegir ahora mismo.");
            Add("es", "select_blocked_reason", "Aun no permitido. {0}");
            Add("es", "inspect_opened", "Inspeccionando {0}. Escape para cerrar.");
            Add("es", "inspect_closed", "Inspeccion cerrada.");
            Add("es", "nothing_to_inspect", "No hay nada que inspeccionar aqui.");
            Add("es", "help_event", "Pantalla de evento. Un evento de historia en tu viaje; su titulo y texto se leen cuando aparecen. Las flechas mueven entre elementos, Enter activa. I inspecciona la carta enfocada, como el clic derecho para jugadores videntes, Escape cierra la inspeccion.");

            // ----- French -------------------------------------------------------

            Add("fr", "stat_health", "{0} points de vie");
            Add("fr", "stat_attack", "{0} d'attaque");
            Add("fr", "stat_counter", "compteur {0}");
            Add("fr", "pocket_draw", "Pioche, {0} cartes");
            Add("fr", "pocket_draw_one", "Pioche, 1 carte");
            Add("fr", "pocket_discard", "Defausse, {0} cartes");
            Add("fr", "pocket_discard_one", "Defausse, 1 carte");
            Add("fr", "card_count_multiple", "{0}, {1} exemplaires");
            Add("fr", "no_item_focused", "Rien de selectionne.");
            Add("fr", "no_info_available", "Aucune information disponible.");
            Add("fr", "gold_amount", "Or: {0}.");
            Add("fr", "scene_CharacterSelect", "Selection du personnage.");
            Add("fr", "scene_Cards", "Collection de cartes.");
            Add("fr", "scene_Mods", "Mods.");
            Add("fr", "scene_Credits", "Credits.");
            Add("fr", "scene_TownUnlocks", "Deblocages du village.");
            Add("fr", "scene_Event", "Evenement.");
            Add("fr", "scene_BossReward", "Recompense de boss.");
            Add("fr", "scene_BattleWin", "Bataille gagnee! Ecran de victoire.");
            Add("fr", "scene_CampaignEnd", "Fin du voyage.");
            Add("fr", "battlewin_continue", "Appuyez sur Entree pour continuer.");
            Add("fr", "battlewin_injuries", "Blesses dans cette bataille: {0}.");
            Add("fr", "battlewin_not_ready", "L'ecran de victoire est encore en train d'apparaitre, un instant.");
            Add("fr", "help_battlewin", "Ecran de victoire apres une bataille gagnee. Peut afficher les compagnons blesses au combat. Entree continue le voyage.");
            Add("fr", "charselect_leaders", "Choisissez votre chef. Les fleches changent de chef, Entree selectionne.");
            Add("fr", "charselect_chosen", "{0} choisi. Entree confirme, Echap remet la carte.");
            Add("fr", "charselect_chosen_generic", "Carte choisie. Entree confirme, Echap la remet.");
            Add("fr", "charselect_cancelled", "Choix annule, retour a la liste.");
            Add("fr", "charselect_pets", "Choisissez votre familier de depart. Les fleches changent, Entree selectionne.");
            Add("fr", "charselect_starting", "C'est parti! Le voyage commence.");
            Add("fr", "inspect_no_confirm", "Ce panneau ne peut pas etre confirme ici. Echap remet la carte.");
            Add("fr", "help_charselect", "Selection du personnage. Les fleches changent d'option et Entree selectionne. Apres la selection, Entree confirme et continue, Echap remet la carte.");
            Add("fr", "help_main_menu", "Menu principal. Fleches haut et bas pour changer de bouton, Entree pour selectionner. F1 repete cette aide, F10 bascule le mode debogage.");
            Add("fr", "screen_town", "Village, votre camp de base.");
            Add("fr", "town_hint", "Les fleches changent de batiment, Entree selectionne, I decrit le batiment selectionne. F1 pour l'aide.");
            Add("fr", "gate_continue_tutorial", "Votre voyage tutoriel est en cours. Appuyez sur Entree pour le continuer");
            Add("fr", "gate_start_tutorial", "Commence votre premier voyage. Le jeu vous proposera le tutoriel");
            Add("fr", "gate_continue_run", "Votre voyage est en cours. Appuyez sur Entree pour le continuer");
            Add("fr", "gate_start_run", "Commence un nouveau voyage");
            Add("fr", "help_town", "Village. Votre base entre les voyages. Les batiments debloquent cartes et defis. Les fleches changent de batiment, I lit sa fonction, Entree l'ouvre. La Porte commence ou continue votre voyage.");
            Add("fr", "screen_continue_run", "Continuer le voyage. Vous avez un voyage en cours.");
            Add("fr", "continue_missing_data", "Ce voyage utilise du contenu manquant et ne peut pas continuer.");
            Add("fr", "continue_started", "Commence le {0}.");
            Add("fr", "continue_leader", "Votre chef: {0}.");
            Add("fr", "continue_deck", "Deck de {0} cartes: {1}.");
            Add("fr", "continue_hint", "Les fleches parcourent cartes et boutons. Entree sur C'est parti continue le voyage.");
            Add("fr", "continue_button_desc", "Continue votre voyage");
            Add("fr", "continue_back_desc", "Retourne au village");
            Add("fr", "help_continue_run", "Ecran de reprise du voyage. Il montre le voyage en cours: chef, deck et date de depart. Entree sur le bouton continuer reprend le voyage. Le bouton retour ramene au village. Abandonner supprime le voyage.");
            Add("fr", "screen_map", "Carte du voyage.");
            Add("fr", "map_zone", "Zone: {0}.");
            Add("fr", "map_you_are_at", "Vous etes a {0}.");
            Add("fr", "map_destinations", "{0} destinations: {1}.");
            Add("fr", "map_hint", "Fleches gauche et droite parcourent le chemin, Entree voyage. M lit toute la carte, I lit les details, G lit l'or.");
            Add("fr", "map_node_here", "vous etes ici");
            Add("fr", "map_node_enter", "Entree pour y entrer");
            Add("fr", "map_only_location", "C'est le seul lieu revele pour le moment.");
            Add("fr", "map_no_controls", "Rien d'autre sur cet ecran.");
            Add("fr", "map_node_cleared", "termine");
            Add("fr", "map_node_available", "disponible, appuyez sur Entree pour y aller");
            Add("fr", "map_node_available_short", "disponible");
            Add("fr", "map_node_ahead", "plus loin");
            Add("fr", "map_node_not_reachable", "inaccessible");
            Add("fr", "map_battle_waves", "{0} vagues");
            Add("fr", "map_not_ready", "La carte n'est pas encore prete.");
            Add("fr", "map_overview", "Apercu de la carte, {0} lieux connus.");
            Add("fr", "map_hidden_nodes", "{0} autres lieux non reveles");
            Add("fr", "map_wave_enemies", "Vague {0}: {1}");
            Add("fr", "help_map", "Carte du voyage. Votre voyage est un chemin de lieux. Fleches gauche et droite changent de lieu. Entree voyage vers un lieu disponible. Haut et bas atteignent les piles de cartes et autres controles. M lit toute la carte, I lit les details du lieu selectionne, ennemis compris, G lit votre or.");
            Add("fr", "node_type_boss", "combat de boss");
            Add("fr", "node_type_battle", "combat");
            Add("fr", "node_type_shop", "boutique");
            Add("fr", "node_type_gnomeshop", "boutique gnome");
            Add("fr", "node_type_charm", "evenement de charme");
            Add("fr", "node_type_gold", "tresor");
            Add("fr", "node_type_item", "evenement d'objet");
            Add("fr", "node_type_companion", "evenement de compagnon");
            Add("fr", "node_type_copyitem", "evenement de copie d'objet");
            Add("fr", "node_type_curseitems", "evenement de malediction");
            Add("fr", "node_type_injuredcompanion", "compagnon blesse");
            Add("fr", "node_type_journalpage", "page de journal");
            Add("fr", "node_type_charmshop", "boutique de charmes");
            Add("fr", "node_type_clunkshop", "boutique de bidules");
            Add("fr", "node_type_muncher", "muncher");
            Add("fr", "node_type_event", "evenement");
            Add("fr", "screen_battle", "Bataille!");
            Add("fr", "battle_wave_total", "{0} vagues ennemies.");
            Add("fr", "battle_hand_count", "{0} cartes en main.");
            Add("fr", "battle_hint", "Haut et bas changent entre main, plateaux, cloche et piles. Gauche et droite se deplacent a l'interieur. Entree prend et pose les cartes. F1 pour l'aide de bataille.");
            Add("fr", "battle_your_turn", "Votre tour. {0} cartes en main.");
            Add("fr", "battle_resolving", "Resolution du tour.");
            Add("fr", "battle_over", "Bataille terminee.");
            Add("fr", "battle_turn", "Tour {0}.");
            Add("fr", "battle_bell_rung", "Cloche de pioche sonnee. Nouvelle main.");
            Add("fr", "battle_group_empty", "{0} est vide.");
            Add("fr", "battle_nothing_to_focus", "Rien a selectionner.");
            Add("fr", "group_hand", "Main");
            Add("fr", "group_your_board", "Votre plateau");
            Add("fr", "group_enemy_board", "Plateau ennemi");
            Add("fr", "group_system", "Cloche et piles");
            Add("fr", "battle_card_picked_up", "{0} en main.");
            Add("fr", "battle_pickup_hint", "Les fleches choisissent une cible, Entree la pose.");
            Add("fr", "battle_card_released", "{0} posee.");
            Add("fr", "battle_invalid_target", "Cible non valide.");
            Add("fr", "battle_cannot_play", "Impossible de jouer cette carte maintenant.");
            Add("fr", "battle_bell_not_ready", "La cloche de pioche n'est pas disponible maintenant.");
            Add("fr", "battle_hand_empty", "Votre main est vide.");
            Add("fr", "battle_acts_in", "agit dans {0}");
            Add("fr", "battle_no_waves", "Pas d'informations de vagues.");
            Add("fr", "battle_wave_n", "Vague {0}: {1}");
            Add("fr", "battle_boss_wave", "vague de boss");
            Add("fr", "battle_all_waves_spawned", "Toutes les vagues sont apparues.");
            Add("fr", "battle_next_wave", "Prochaine vague dans {0} tours.");
            Add("fr", "battle_bell_charged", "La cloche de pioche est chargee. La sonner pioche une nouvelle main gratuitement.");
            Add("fr", "battle_bell_charging", "La cloche de pioche sera prete dans {0} tours. La sonner maintenant utilise votre tour.");
            Add("fr", "battle_bell_counter", "Chargee dans {0} tours.");
            Add("fr", "battle_phase_play", "A vous de jouer.");
            Add("fr", "battle_phase_other", "En attente.");
            Add("fr", "battle_hit", "{0} frappe {1} pour {2}.");
            Add("fr", "battle_takes_damage", "{0} subit {1} degats.");
            Add("fr", "battle_healed", "{0} recupere {1} points de vie.");
            Add("fr", "battle_dodged", "{0} a esquive.");
            Add("fr", "battle_destroyed", "{0} detruit.");
            Add("fr", "battle_status_applied", "{0} {1} applique a {2}.");
            Add("fr", "help_battle", "Bataille. Haut et bas changent de groupe: main, votre plateau, plateau ennemi, cloche et piles. Gauche et droite se deplacent dans le groupe. Entree sur une carte de la main la prend, les fleches choisissent une cible, Entree la pose. Entree sur une de vos unites du plateau la prend pour la deplacer: une case libre la deplace, une case occupee echange ou pousse, la zone de rappel la retire du plateau. Deplacer et rappeler sont des actions gratuites qui ne terminent pas votre tour. Echap repose une carte prise. Jouer une carte ou sonner la cloche termine votre tour. Touches de lecture: H main, B plateau, W vagues, R cloche, T tour, G or, M cloches de modificateur. Appuyez sur O pour le menu du jeu avec les reglages.");
            Add("fr", "battle_unit_picked_up", "{0} prise du plateau.");
            Add("fr", "battle_move_hint", "Les fleches choisissent une case de destination ou la zone de rappel, Entree confirme, Echap annule.");
            Add("fr", "battle_unit_moved", "{0} deplacee.");
            Add("fr", "battle_unit_recalled", "{0} rappelee.");
            Add("fr", "battle_free_action", "Action gratuite, votre tour continue.");
            Add("fr", "battle_pickup_cancelled", "{0} reposee.");
            Add("fr", "battle_cannot_move", "Cette unite ne peut pas etre deplacee maintenant.");
            Add("fr", "battle_recall_zone", "Zone de rappel. Deposez ici pour rappeler l'unite.");
            Add("fr", "battle_play_anchor", "Zone de jeu. Entree joue la carte sans cible.");
            Add("fr", "battle_trigger_snowed", "{0} est enneigee et ne peut pas agir.");
            Add("fr", "battle_trigger_nullified", "L'action de {0} a ete annulee.");
            Add("fr", "battle_trigger_smackback", "{0} riposte contre {1}!");
            Add("fr", "battle_trigger_laststand", "{0} agit dans un dernier souffle!");
            Add("fr", "battle_trigger_chain", "{0} est declenchee par {1}.");
            Add("fr", "battle_trigger_acts", "{0} agit.");
            Add("fr", "battle_kill_combo", "Combo x{0}!");
            Add("fr", "battle_gold_dropped", "{0} d'or.");
            Add("fr", "battle_crown_deploy_one", "Une carte couronnee dans votre main se deploie avant la bataille: appuyez sur Entree dessus pour la placer maintenant.");
            Add("fr", "battle_crown_deploy", "{0} cartes couronnees dans votre main se deploient avant la bataille: placez-les maintenant.");
            Add("fr", "card_crowned", "Couronnee, se deploie au debut de la bataille.");
            Add("fr", "card_charm_one", "Talisman: {0}.");
            Add("fr", "card_charms", "{0} talismans: {1}.");
            Add("fr", "card_token_one", "Jeton: {0}.");
            Add("fr", "card_tokens", "{0} jetons: {1}.");
            Add("fr", "counter_frozen", "compteur gele par la neige");
            Add("fr", "card_injured_one", "Blesse.");
            Add("fr", "card_injured", "Blesse x{0}.");
            Add("fr", "card_mentions", "Mentionne {0}");
            Add("fr", "upgrade_charm", "talisman");
            Add("fr", "upgrade_crown", "couronne");
            Add("fr", "upgrade_token", "jeton");
            Add("fr", "battle_last_stand", "Dernier combat! {0} refuse de tomber. La bataille se joue aux des. Appuyez sur Entree pour lancer les des.");
            Add("fr", "battle_last_stand_generic", "Dernier combat! La bataille se joue aux des. Appuyez sur Entree pour lancer les des.");
            Add("fr", "battle_last_stand_rolling", "Lancer des des.");
            Add("fr", "battle_last_stand_won", "Vous gagnez le dernier combat!");
            Add("fr", "battle_last_stand_lost", "Vous perdez le dernier combat.");
            Add("fr", "battle_companion_injured", "{0} a ete blesse!");
            Add("fr", "crown_holder_empty", "Support de couronne, vide. La couronne a deja ete prise.");
            Add("fr", "battle_bell_name", "Cloche de pioche");
            Add("fr", "battle_wave_bell_name", "Cloche de vague");
            Add("fr", "battle_wave_incoming", "{0} ennemis arrivent dans {1} tours.");
            Add("fr", "battle_wave_overflow", "{0} d'entre eux ne tiendront pas sur le plateau ennemi.");
            Add("fr", "battle_wave_call_early", "Peut etre sonnee pour appeler la vague plus tot.");
            Add("fr", "battle_wave_call_reward", "Recompense si sonnee maintenant: {0} d'or.");
            Add("fr", "battle_no_modifiers", "Aucune cloche de modificateur active.");
            Add("fr", "battle_modifier_bell", "Cloche de modificateur.");
            Add("fr", "screen_pause", "Menu du jeu.");
            Add("fr", "pause_hint", "Haut et bas parcourent la page, Entree active. Gauche et droite changent d'onglet, ou modifient la valeur d'un reglage. T va aux onglets, Echap revient en arriere. Appuyez sur O pour fermer le menu.");
            Add("fr", "pause_no_tabs", "Aucun onglet accessible ici. Echap revient en arriere.");
            Add("fr", "pause_tab_named", "{0}, onglet. Entree pour ouvrir.");
            Add("fr", "pause_tab", "Onglet. Entree pour ouvrir.");
            Add("fr", "pause_tab_opened", "{0} ouvert.");
            Add("fr", "pause_closed", "Menu ferme.");
            Add("fr", "pause_unavailable", "Le menu n'est pas disponible maintenant.");
            Add("fr", "setting_adjust_hint", "Gauche et droite changent la valeur.");
            Add("fr", "setting_percent", "{0} pour cent");
            Add("fr", "nav_nothing", "Rien a selectionner ici.");
            Add("fr", "row_not_interactive", "Cette entree est en lecture seule.");
            Add("fr", "pause_lore_page", "Page d'histoire");
            Add("fr", "pause_lore_locked", "verrouillee");
            Add("fr", "pause_lore_new", "nouvelle");
            Add("fr", "pause_lore_open_hint", "Entree pour lire.");
            Add("fr", "pause_lore_close_hint", "Echap ferme la page.");
            Add("fr", "pause_lore_closed", "Page fermee.");
            Add("fr", "stat_no_value", "aucun");
            Add("fr", "help_pause", "Menu du jeu. Haut et bas parcourent les elements de la page, Entree active. Gauche et droite parcourent les onglets, ou modifient la valeur d'un reglage. T va aux onglets. Echap revient d'un niveau, par exemple hors d'une categorie de reglages. Tab et Maj Tab parcourent aussi la page. Appuyez sur O pour fermer le menu.");
            Add("fr", "event_prompt_action", "Appuyez sur Entree.");
            Add("fr", "event_crack", "Fissure {0} sur 4.");
            Add("fr", "select_blocked", "Impossible de choisir ceci pour le moment.");
            Add("fr", "select_blocked_reason", "Pas encore autorise. {0}");
            Add("fr", "inspect_opened", "Inspection de {0}. Echap pour fermer.");
            Add("fr", "inspect_closed", "Inspection fermee.");
            Add("fr", "nothing_to_inspect", "Rien a inspecter ici.");
            Add("fr", "help_event", "Ecran d'evenement. Un evenement d'histoire sur votre voyage; son titre et son texte sont lus quand ils apparaissent. Les fleches naviguent entre les elements, Entree active. I inspecte la carte selectionnee, comme le clic droit pour les joueurs voyants, Echap ferme l'inspection.");
        }
    }
}
