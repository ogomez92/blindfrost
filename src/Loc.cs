using System;
using System.Collections.Generic;
using System.IO;
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

        // When set, overrides the game locale for all mod speech (language.txt)
        private static string _overrideLang;

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
        /// Apply the language override from [modDirectory]/language.txt, if
        /// present. The game itself only offers English, Japanese, Korean and
        /// Chinese, so the mod's other translations (Spanish, German,
        /// French...) can only be reached this way. The file holds a single
        /// language code such as "es"; a missing or empty file follows the
        /// game's language setting. Must run after Initialize().
        /// </summary>
        public static void LoadLanguageOverride(string modDirectory)
        {
            try
            {
                string path = Path.Combine(modDirectory, "language.txt");
                if (!File.Exists(path))
                    return;

                string code = File.ReadAllText(path).Trim();
                if (code.Length == 0)
                    return;

                // Normalize case against the registered languages ("ES" → "es",
                // "zh-hans" → "zh-Hans"); unknown codes follow the game language
                foreach (string lang in _strings.Keys)
                {
                    if (string.Equals(lang, code, StringComparison.OrdinalIgnoreCase))
                    {
                        _overrideLang = lang;
                        return;
                    }
                }
                DebugLogger.Log(DebugLogger.LogCategory.ScreenReader,
                    $"language.txt has unknown code '{code}'; following the game language");
            }
            catch
            {
                // Unreadable file — follow the game language
            }
        }

        /// <summary>
        /// Returns the locale code of the game's currently selected language,
        /// unless language.txt overrides it.
        /// </summary>
        public static string GetCurrentLanguageCode()
        {
            if (_overrideLang != null)
                return _overrideLang;

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
            Add("en", "help_text", "Wildfrost Accessibility. F1: this help. F10: toggle debug mode. Arrow keys: navigate. Enter: activate. Control plus arrow keys: review buffers. V: short or full descriptions.");
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
            Add("de", "help_text", "Wildfrost Barrierefreiheit. F1: diese Hilfe. F10: Debug-Modus umschalten. Pfeiltasten: navigieren. Enter: auswaehlen. Strg plus Pfeiltasten: Lesepuffer. V: kurze oder ausfuehrliche Beschreibungen.");
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
            Add("fr", "help_text", "Accessibilite Wildfrost. F1: cette aide. F10: basculer le mode debogage. Fleches: naviguer. Entree: selectionner. Ctrl plus fleches: tampons de lecture. V: descriptions courtes ou completes.");
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
            Add("es", "help_text", "Accesibilidad de Wildfrost. F1: esta ayuda. F10: alternar modo de depuracion. Flechas: navegar. Enter: seleccionar. Ctrl mas flechas: buferes de revision. V: descripciones cortas o completas.");
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
            Add("ja", "help_text", "Wildfrost アクセシビリティ。F1: ヘルプ。F10: デバッグモード切替。矢印キー: 移動。Enter: 決定。Ctrl+矢印キー: レビューバッファ。V: 説明の長さ切替。");
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
            Add("ko", "help_text", "Wildfrost 접근성. F1: 도움말. F10: 디버그 모드 전환. 방향키: 탐색. Enter: 선택. Ctrl+방향키: 검토 버퍼. V: 설명 길이 전환.");
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
            Add("zh-Hans", "help_text", "Wildfrost 无障碍。F1: 帮助。F10: 切换调试模式。方向键: 导航。回车: 确认。Ctrl加方向键: 查看缓冲区。V: 切换描述长短。");
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
            Add("zh-Hant", "help_text", "Wildfrost 無障礙。F1: 說明。F10: 切換偵錯模式。方向鍵: 導航。Enter: 確認。Ctrl加方向鍵: 檢視緩衝區。V: 切換描述長短。");
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
            Add("it", "help_text", "Accessibilita Wildfrost. F1: aiuto. F10: attiva/disattiva debug. Frecce: navigare. Invio: selezionare. Ctrl piu frecce: buffer di revisione. V: descrizioni brevi o complete.");
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
            Add("pt", "help_text", "Acessibilidade Wildfrost. F1: ajuda. F10: alternar modo de depuracao. Setas: navegar. Enter: selecionar. Ctrl mais setas: buffers de revisao. V: descricoes curtas ou completas.");
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
            Add("ru", "help_text", "Доступность Wildfrost. F1: справка. F10: режим отладки. Стрелки: навигация. Enter: выбрать. Ctrl со стрелками: буферы просмотра. V: краткие или полные описания.");
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
            Add("pl", "help_text", "Dostepnosc Wildfrost. F1: pomoc. F10: tryb debugowania. Strzalki: nawigacja. Enter: wybierz. Ctrl plus strzalki: bufory przegladu. V: krotkie lub pelne opisy.");
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
            Add("tr", "help_text", "Wildfrost Erisilebilirlik. F1: yardim. F10: hata ayiklama modu. Ok tuslari: gezinme. Enter: sec. Ctrl arti ok tuslari: inceleme arabellekleri. V: kisa veya tam aciklamalar.");
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

            RegisterReviewBufferStrings();
            RegisterHandlerStrings();
            RegisterOverlayAndNarrationStrings();
        }

        /// <summary>
        /// Strings for the overlay scenes (boss rewards, card frames, town
        /// unlocks, Frostoscope, journal, credits) and the visual-moment
        /// narration (speech bubbles, miniboss arrivals, the final-boss shade).
        /// </summary>
        private static void RegisterOverlayAndNarrationStrings()
        {
            // ----- English -----------------------------------------------------
            Add("en", "scene_CardFramesUnlocked", "Card frames unlocked!");
            Add("en", "scene_NewFrostGuardian", "The Frostoscope reveals the next final boss awaiting your tribe.");
            Add("en", "scene_JournalNameHistory", "The journal of leaders.");
            Add("en", "scene_DemoEnd", "End of the demo.");
            Add("en", "overlay_continue_hint", "Press Enter to continue.");
            Add("en", "help_overlay_continue", "A short celebration screen. Press Enter or Escape to continue.");

            Add("en", "creditsend_announce", "The Frost is vanquished! The end credits roll.");
            Add("en", "credits_skip_hint", "Press Enter or Escape to skip.");
            Add("en", "credits_skipped", "Credits skipped.");

            Add("en", "townunlock_gained", "Unlocked: {0}.");
            Add("en", "frostoscope_nothing", "The scope shows nothing right now.");
            Add("en", "frostoscope_roster", "Through the frosty lens you see: {0}.");

            Add("en", "journal_name_add", "The journal opens, and your leader's name is written into the history of leaders.");
            Add("en", "journal_name_add_named", "The journal opens, and {0}'s name is written into the history of leaders.");
            Add("en", "journal_name_void", "The journal opens, and the fallen leader's name is crossed out.");

            Add("en", "bossreward_open_hint", "Press Enter to open it.");
            Add("en", "bossreward_choose_hint", "The rewards are revealed. Use the arrow keys to hear each one, Enter takes it.");
            Add("en", "bossreward_not_ready", "Still opening, one moment.");
            Add("en", "bossreward_opening", "It creaks open...");
            Add("en", "bossreward_option", "Option {0} of {1}: {2}");
            Add("en", "bossreward_taken", "Taken: {0}.");
            Add("en", "bossreward_take_failed", "Could not take this reward. Try the arrow keys and Enter again.");
            Add("en", "bossreward_pick_first", "Use the arrow keys to choose a reward first.");
            Add("en", "help_bossreward", "Boss reward. A sealed prize opens into a choice of blessings. Enter opens it, arrows browse the rewards, Enter takes one.");

            Add("en", "event_card_picked_up", "{0} picked up.");
            Add("en", "event_card_released", "{0} given.");
            Add("en", "event_pickup_hint", "Arrows choose a target, Enter gives the card to it, Escape puts it back.");

            Add("en", "campaignend_press_again", "Press Enter again to activate, or arrows for other buttons.");
            Add("en", "help_generic_screen", "This screen has no dedicated support yet. Arrow keys move between elements, Enter activates, I inspects a focused card, P opens the inventory.");

            Add("en", "speech_bubble", "{0} says: {1}");
            Add("en", "narrate_miniboss", "A powerful enemy slams onto the battlefield: {0}!");
            Add("en", "narrate_wave", "The wave bell tolls! New enemies charge onto the battlefield.");
            Add("en", "narrate_boss_transform", "{0} explodes and transforms! The battle enters a new phase.");
            Add("en", "narrate_shade_flee", "A dark wisp rises from the fallen guardian and flees into the storm.");
            Add("en", "narrate_shade_possess", "A dark wisp rises from the fallen guardian and dives toward your leader!");
            Add("en", "narrate_shade_blocked", "{0} blocks the dark wisp! The Frost is sealed away.");
            Add("en", "narrate_frost_eyes", "{0}'s eyes glow with frost. The Frost has taken hold.");
            Add("en", "narrate_combine", "Your cards swirl together and combine into something new!");
            Add("en", "charselect_tribes", "Choose your tribe. Use the arrow keys to browse the tribe flags, Enter picks one.");

            // ----- German ------------------------------------------------------
            Add("de", "scene_CardFramesUnlocked", "Kartenrahmen freigeschaltet!");
            Add("de", "scene_NewFrostGuardian", "Das Frostoskop zeigt den naechsten Endboss, der auf deinen Stamm wartet.");
            Add("de", "scene_JournalNameHistory", "Das Journal der Anfuehrer.");
            Add("de", "scene_DemoEnd", "Ende der Demo.");
            Add("de", "overlay_continue_hint", "Druecke Eingabe um fortzufahren.");
            Add("de", "help_overlay_continue", "Ein kurzer Feierbildschirm. Eingabe oder Escape setzt fort.");

            Add("de", "creditsend_announce", "Der Frost ist bezwungen! Der Abspann laeuft.");
            Add("de", "credits_skip_hint", "Eingabe oder Escape ueberspringt.");
            Add("de", "credits_skipped", "Abspann uebersprungen.");

            Add("de", "townunlock_gained", "Freigeschaltet: {0}.");
            Add("de", "frostoscope_nothing", "Das Fernrohr zeigt gerade nichts.");
            Add("de", "frostoscope_roster", "Durch die frostige Linse siehst du: {0}.");

            Add("de", "journal_name_add", "Das Journal oeffnet sich, und der Name deines Anfuehrers wird in die Geschichte der Anfuehrer geschrieben.");
            Add("de", "journal_name_add_named", "Das Journal oeffnet sich, und {0}s Name wird in die Geschichte der Anfuehrer geschrieben.");
            Add("de", "journal_name_void", "Das Journal oeffnet sich, und der Name des gefallenen Anfuehrers wird durchgestrichen.");

            Add("de", "bossreward_open_hint", "Druecke Eingabe um sie zu oeffnen.");
            Add("de", "bossreward_choose_hint", "Die Belohnungen sind enthuellt. Pfeiltasten lesen jede vor, Eingabe nimmt sie.");
            Add("de", "bossreward_not_ready", "Oeffnet sich noch, einen Moment.");
            Add("de", "bossreward_opening", "Sie knarrt auf...");
            Add("de", "bossreward_option", "Option {0} von {1}: {2}");
            Add("de", "bossreward_taken", "Genommen: {0}.");
            Add("de", "bossreward_take_failed", "Belohnung konnte nicht genommen werden. Versuche Pfeiltasten und Eingabe erneut.");
            Add("de", "bossreward_pick_first", "Waehle zuerst mit den Pfeiltasten eine Belohnung.");
            Add("de", "help_bossreward", "Boss-Belohnung. Ein versiegelter Preis oeffnet sich zu einer Auswahl an Segen. Eingabe oeffnet ihn, Pfeile durchstoebern die Belohnungen, Eingabe nimmt eine.");

            Add("de", "event_card_picked_up", "{0} aufgenommen.");
            Add("de", "event_card_released", "{0} uebergeben.");
            Add("de", "event_pickup_hint", "Pfeile waehlen ein Ziel, Eingabe gibt die Karte, Escape legt sie zurueck.");

            Add("de", "campaignend_press_again", "Druecke erneut Eingabe zum Aktivieren, oder Pfeile fuer andere Knoepfe.");
            Add("de", "help_generic_screen", "Dieser Bildschirm hat noch keine eigene Unterstuetzung. Pfeiltasten bewegen zwischen Elementen, Eingabe aktiviert, I untersucht eine fokussierte Karte, P oeffnet das Inventar.");

            Add("de", "speech_bubble", "{0} sagt: {1}");
            Add("de", "narrate_miniboss", "Ein maechtiger Gegner kracht aufs Schlachtfeld: {0}!");
            Add("de", "narrate_wave", "Die Wellenglocke laeutet! Neue Gegner stuermen aufs Schlachtfeld.");
            Add("de", "narrate_boss_transform", "{0} explodiert und verwandelt sich! Der Kampf tritt in eine neue Phase.");
            Add("de", "narrate_shade_flee", "Ein dunkler Schemen steigt aus dem gefallenen Waechter und flieht in den Sturm.");
            Add("de", "narrate_shade_possess", "Ein dunkler Schemen steigt aus dem gefallenen Waechter und stuerzt sich auf deinen Anfuehrer!");
            Add("de", "narrate_shade_blocked", "{0} blockt den dunklen Schemen! Der Frost ist versiegelt.");
            Add("de", "narrate_frost_eyes", "{0}s Augen gluehen frostig. Der Frost hat Besitz ergriffen.");
            Add("de", "narrate_combine", "Deine Karten wirbeln zusammen und verschmelzen zu etwas Neuem!");
            Add("de", "charselect_tribes", "Waehle deinen Stamm. Pfeiltasten durchstoebern die Stammesflaggen, Eingabe waehlt eine.");

            // ----- Spanish -----------------------------------------------------
            Add("es", "scene_CardFramesUnlocked", "Marcos de carta desbloqueados!");
            Add("es", "scene_NewFrostGuardian", "El Frostoscopio revela al proximo jefe final que espera a tu tribu.");
            Add("es", "scene_JournalNameHistory", "El diario de lideres.");
            Add("es", "scene_DemoEnd", "Fin de la demo.");
            Add("es", "overlay_continue_hint", "Pulsa Intro para continuar.");
            Add("es", "help_overlay_continue", "Una breve pantalla de celebracion. Intro o Escape continua.");

            Add("es", "creditsend_announce", "La Escarcha ha sido vencida! Ruedan los creditos finales.");
            Add("es", "credits_skip_hint", "Pulsa Intro o Escape para saltarlos.");
            Add("es", "credits_skipped", "Creditos saltados.");

            Add("es", "townunlock_gained", "Desbloqueado: {0}.");
            Add("es", "frostoscope_nothing", "El visor no muestra nada ahora mismo.");
            Add("es", "frostoscope_roster", "A traves de la lente helada ves: {0}.");

            Add("es", "journal_name_add", "El diario se abre y el nombre de tu lider queda escrito en la historia de lideres.");
            Add("es", "journal_name_add_named", "El diario se abre y el nombre de {0} queda escrito en la historia de lideres.");
            Add("es", "journal_name_void", "El diario se abre y el nombre del lider caido queda tachado.");

            Add("es", "bossreward_open_hint", "Pulsa Intro para abrirlo.");
            Add("es", "bossreward_choose_hint", "Las recompensas estan a la vista. Las flechas leen cada una, Intro la toma.");
            Add("es", "bossreward_not_ready", "Todavia se esta abriendo, un momento.");
            Add("es", "bossreward_opening", "Se abre con un crujido...");
            Add("es", "bossreward_option", "Opcion {0} de {1}: {2}");
            Add("es", "bossreward_taken", "Tomada: {0}.");
            Add("es", "bossreward_take_failed", "No se pudo tomar esta recompensa. Prueba las flechas e Intro de nuevo.");
            Add("es", "bossreward_pick_first", "Elige primero una recompensa con las flechas.");
            Add("es", "help_bossreward", "Recompensa de jefe. Un premio sellado se abre en una eleccion de bendiciones. Intro lo abre, las flechas recorren las recompensas, Intro toma una.");

            Add("es", "event_card_picked_up", "{0} en la mano.");
            Add("es", "event_card_released", "{0} entregada.");
            Add("es", "event_pickup_hint", "Las flechas eligen un objetivo, Intro le entrega la carta, Escape la devuelve.");

            Add("es", "campaignend_press_again", "Pulsa Intro otra vez para activar, o las flechas para otros botones.");
            Add("es", "help_generic_screen", "Esta pantalla aun no tiene soporte dedicado. Las flechas mueven entre elementos, Intro activa, I inspecciona la carta enfocada, P abre el inventario.");

            Add("es", "speech_bubble", "{0} dice: {1}");
            Add("es", "narrate_miniboss", "Un enemigo poderoso se estrella en el campo de batalla: {0}!");
            Add("es", "narrate_wave", "Suena la campana de oleada! Nuevos enemigos cargan al campo de batalla.");
            Add("es", "narrate_boss_transform", "{0} explota y se transforma! La batalla entra en una nueva fase.");
            Add("es", "narrate_shade_flee", "Una voluta oscura surge del guardian caido y huye hacia la tormenta.");
            Add("es", "narrate_shade_possess", "Una voluta oscura surge del guardian caido y se lanza hacia tu lider!");
            Add("es", "narrate_shade_blocked", "{0} bloquea la voluta oscura! La Escarcha queda sellada.");
            Add("es", "narrate_frost_eyes", "Los ojos de {0} brillan con escarcha. La Escarcha ha tomado el control.");
            Add("es", "narrate_combine", "Tus cartas giran juntas y se combinan en algo nuevo!");
            Add("es", "charselect_tribes", "Elige tu tribu. Las flechas recorren las banderas de tribu, Intro elige una.");

            // ----- French ------------------------------------------------------
            Add("fr", "scene_CardFramesUnlocked", "Cadres de carte debloques!");
            Add("fr", "scene_NewFrostGuardian", "Le Frostoscope revele le prochain boss final qui attend votre tribu.");
            Add("fr", "scene_JournalNameHistory", "Le journal des chefs.");
            Add("fr", "scene_DemoEnd", "Fin de la demo.");
            Add("fr", "overlay_continue_hint", "Appuyez sur Entree pour continuer.");
            Add("fr", "help_overlay_continue", "Un court ecran de celebration. Entree ou Echap continue.");

            Add("fr", "creditsend_announce", "Le Givre est vaincu! Le generique de fin defile.");
            Add("fr", "credits_skip_hint", "Appuyez sur Entree ou Echap pour passer.");
            Add("fr", "credits_skipped", "Generique passe.");

            Add("fr", "townunlock_gained", "Debloque: {0}.");
            Add("fr", "frostoscope_nothing", "La lunette ne montre rien pour le moment.");
            Add("fr", "frostoscope_roster", "A travers la lentille givree, vous voyez: {0}.");

            Add("fr", "journal_name_add", "Le journal s'ouvre, et le nom de votre chef est inscrit dans l'histoire des chefs.");
            Add("fr", "journal_name_add_named", "Le journal s'ouvre, et le nom de {0} est inscrit dans l'histoire des chefs.");
            Add("fr", "journal_name_void", "Le journal s'ouvre, et le nom du chef tombe est raye.");

            Add("fr", "bossreward_open_hint", "Appuyez sur Entree pour l'ouvrir.");
            Add("fr", "bossreward_choose_hint", "Les recompenses sont revelees. Les fleches lisent chacune, Entree la prend.");
            Add("fr", "bossreward_not_ready", "Ca s'ouvre encore, un instant.");
            Add("fr", "bossreward_opening", "Ca s'ouvre en grincant...");
            Add("fr", "bossreward_option", "Option {0} sur {1}: {2}");
            Add("fr", "bossreward_taken", "Prise: {0}.");
            Add("fr", "bossreward_take_failed", "Impossible de prendre cette recompense. Reessayez avec les fleches et Entree.");
            Add("fr", "bossreward_pick_first", "Choisissez d'abord une recompense avec les fleches.");
            Add("fr", "help_bossreward", "Recompense de boss. Un prix scelle s'ouvre sur un choix de benedictions. Entree l'ouvre, les fleches parcourent les recompenses, Entree en prend une.");

            Add("fr", "event_card_picked_up", "{0} en main.");
            Add("fr", "event_card_released", "{0} donnee.");
            Add("fr", "event_pickup_hint", "Les fleches choisissent une cible, Entree lui donne la carte, Echap la reprend.");

            Add("fr", "campaignend_press_again", "Appuyez encore sur Entree pour activer, ou les fleches pour d'autres boutons.");
            Add("fr", "help_generic_screen", "Cet ecran n'a pas encore de prise en charge dediee. Les fleches se deplacent entre les elements, Entree active, I inspecte la carte selectionnee, P ouvre l'inventaire.");

            Add("fr", "speech_bubble", "{0} dit: {1}");
            Add("fr", "narrate_miniboss", "Un ennemi puissant s'ecrase sur le champ de bataille: {0}!");
            Add("fr", "narrate_wave", "La cloche de vague sonne! De nouveaux ennemis chargent sur le champ de bataille.");
            Add("fr", "narrate_boss_transform", "{0} explose et se transforme! La bataille entre dans une nouvelle phase.");
            Add("fr", "narrate_shade_flee", "Une volute sombre s'eleve du gardien tombe et s'enfuit dans la tempete.");
            Add("fr", "narrate_shade_possess", "Une volute sombre s'eleve du gardien tombe et fonce vers votre chef!");
            Add("fr", "narrate_shade_blocked", "{0} bloque la volute sombre! Le Givre est scelle.");
            Add("fr", "narrate_frost_eyes", "Les yeux de {0} brillent de givre. Le Givre s'est empare de lui.");
            Add("fr", "narrate_combine", "Vos cartes tourbillonnent et fusionnent en quelque chose de nouveau!");
            Add("fr", "charselect_tribes", "Choisissez votre tribu. Les fleches parcourent les drapeaux de tribu, Entree en choisit un.");
        }

        /// <summary>
        /// Review buffer strings (Ctrl+arrows) and the V verbosity toggle,
        /// for every core language.
        /// </summary>
        private static void RegisterReviewBufferStrings()
        {
            // English
            Add("en", "buffer_events", "Events");
            Add("en", "buffer_details", "Details");
            Add("en", "buffer_hand", "Hand");
            Add("en", "buffer_board", "Board");
            Add("en", "buffer_resources", "Resources");
            Add("en", "buffer_waves", "Waves");
            Add("en", "buffer_map", "Map locations");
            Add("en", "buffer_switched", "{0}, {1} items");
            Add("en", "buffer_switched_one", "{0}, 1 item");
            Add("en", "buffer_none", "Nothing to review");
            Add("en", "verbosity_short", "Short descriptions. Details are in the review buffers.");
            Add("en", "verbosity_verbose", "Full descriptions");

            // German
            Add("de", "buffer_events", "Ereignisse");
            Add("de", "buffer_details", "Details");
            Add("de", "buffer_hand", "Hand");
            Add("de", "buffer_board", "Spielfeld");
            Add("de", "buffer_resources", "Ressourcen");
            Add("de", "buffer_waves", "Wellen");
            Add("de", "buffer_map", "Orte auf der Karte");
            Add("de", "buffer_switched", "{0}, {1} Eintraege");
            Add("de", "buffer_switched_one", "{0}, 1 Eintrag");
            Add("de", "buffer_none", "Nichts zum Nachlesen");
            Add("de", "verbosity_short", "Kurze Beschreibungen. Details stehen in den Lesepuffern.");
            Add("de", "verbosity_verbose", "Ausfuehrliche Beschreibungen");

            // French
            Add("fr", "buffer_events", "Evenements");
            Add("fr", "buffer_details", "Details");
            Add("fr", "buffer_hand", "Main");
            Add("fr", "buffer_board", "Plateau");
            Add("fr", "buffer_resources", "Ressources");
            Add("fr", "buffer_waves", "Vagues");
            Add("fr", "buffer_map", "Lieux de la carte");
            Add("fr", "buffer_switched", "{0}, {1} elements");
            Add("fr", "buffer_switched_one", "{0}, 1 element");
            Add("fr", "buffer_none", "Rien a relire");
            Add("fr", "verbosity_short", "Descriptions courtes. Les details sont dans les tampons de lecture.");
            Add("fr", "verbosity_verbose", "Descriptions completes");

            // Spanish
            Add("es", "buffer_events", "Eventos");
            Add("es", "buffer_details", "Detalles");
            Add("es", "buffer_hand", "Mano");
            Add("es", "buffer_board", "Tablero");
            Add("es", "buffer_resources", "Recursos");
            Add("es", "buffer_waves", "Oleadas");
            Add("es", "buffer_map", "Lugares del mapa");
            Add("es", "buffer_switched", "{0}, {1} elementos");
            Add("es", "buffer_switched_one", "{0}, 1 elemento");
            Add("es", "buffer_none", "Nada que revisar");
            Add("es", "verbosity_short", "Descripciones cortas. Los detalles estan en los buferes de revision.");
            Add("es", "verbosity_verbose", "Descripciones completas");

            // Japanese
            Add("ja", "buffer_events", "イベント");
            Add("ja", "buffer_details", "詳細");
            Add("ja", "buffer_hand", "手札");
            Add("ja", "buffer_board", "盤面");
            Add("ja", "buffer_resources", "リソース");
            Add("ja", "buffer_waves", "ウェーブ");
            Add("ja", "buffer_map", "マップの場所");
            Add("ja", "buffer_switched", "{0}、{1}件");
            Add("ja", "buffer_switched_one", "{0}、1件");
            Add("ja", "buffer_none", "確認できる情報はありません");
            Add("ja", "verbosity_short", "短い説明。詳細はレビューバッファにあります。");
            Add("ja", "verbosity_verbose", "完全な説明");

            // Korean
            Add("ko", "buffer_events", "이벤트");
            Add("ko", "buffer_details", "상세 정보");
            Add("ko", "buffer_hand", "손패");
            Add("ko", "buffer_board", "전장");
            Add("ko", "buffer_resources", "자원");
            Add("ko", "buffer_waves", "웨이브");
            Add("ko", "buffer_map", "지도 위치");
            Add("ko", "buffer_switched", "{0}, {1}개 항목");
            Add("ko", "buffer_switched_one", "{0}, 1개 항목");
            Add("ko", "buffer_none", "검토할 내용 없음");
            Add("ko", "verbosity_short", "짧은 설명. 자세한 내용은 검토 버퍼에 있습니다.");
            Add("ko", "verbosity_verbose", "전체 설명");

            // Simplified Chinese
            Add("zh-Hans", "buffer_events", "事件");
            Add("zh-Hans", "buffer_details", "详情");
            Add("zh-Hans", "buffer_hand", "手牌");
            Add("zh-Hans", "buffer_board", "战场");
            Add("zh-Hans", "buffer_resources", "资源");
            Add("zh-Hans", "buffer_waves", "波次");
            Add("zh-Hans", "buffer_map", "地图地点");
            Add("zh-Hans", "buffer_switched", "{0}，{1}项");
            Add("zh-Hans", "buffer_switched_one", "{0}，1项");
            Add("zh-Hans", "buffer_none", "没有可查看的内容");
            Add("zh-Hans", "verbosity_short", "简短描述。详细信息在查看缓冲区中。");
            Add("zh-Hans", "verbosity_verbose", "完整描述");

            // Traditional Chinese
            Add("zh-Hant", "buffer_events", "事件");
            Add("zh-Hant", "buffer_details", "詳情");
            Add("zh-Hant", "buffer_hand", "手牌");
            Add("zh-Hant", "buffer_board", "戰場");
            Add("zh-Hant", "buffer_resources", "資源");
            Add("zh-Hant", "buffer_waves", "波次");
            Add("zh-Hant", "buffer_map", "地圖地點");
            Add("zh-Hant", "buffer_switched", "{0}，{1}項");
            Add("zh-Hant", "buffer_switched_one", "{0}，1項");
            Add("zh-Hant", "buffer_none", "沒有可查看的內容");
            Add("zh-Hant", "verbosity_short", "簡短描述。詳細資訊在檢視緩衝區中。");
            Add("zh-Hant", "verbosity_verbose", "完整描述");

            // Italian
            Add("it", "buffer_events", "Eventi");
            Add("it", "buffer_details", "Dettagli");
            Add("it", "buffer_hand", "Mano");
            Add("it", "buffer_board", "Campo");
            Add("it", "buffer_resources", "Risorse");
            Add("it", "buffer_waves", "Ondate");
            Add("it", "buffer_map", "Luoghi della mappa");
            Add("it", "buffer_switched", "{0}, {1} elementi");
            Add("it", "buffer_switched_one", "{0}, 1 elemento");
            Add("it", "buffer_none", "Niente da rivedere");
            Add("it", "verbosity_short", "Descrizioni brevi. I dettagli sono nei buffer di revisione.");
            Add("it", "verbosity_verbose", "Descrizioni complete");

            // Portuguese
            Add("pt", "buffer_events", "Eventos");
            Add("pt", "buffer_details", "Detalhes");
            Add("pt", "buffer_hand", "Mao");
            Add("pt", "buffer_board", "Tabuleiro");
            Add("pt", "buffer_resources", "Recursos");
            Add("pt", "buffer_waves", "Ondas");
            Add("pt", "buffer_map", "Locais do mapa");
            Add("pt", "buffer_switched", "{0}, {1} itens");
            Add("pt", "buffer_switched_one", "{0}, 1 item");
            Add("pt", "buffer_none", "Nada para rever");
            Add("pt", "verbosity_short", "Descricoes curtas. Os detalhes estao nos buffers de revisao.");
            Add("pt", "verbosity_verbose", "Descricoes completas");

            // Russian
            Add("ru", "buffer_events", "События");
            Add("ru", "buffer_details", "Подробности");
            Add("ru", "buffer_hand", "Рука");
            Add("ru", "buffer_board", "Поле");
            Add("ru", "buffer_resources", "Ресурсы");
            Add("ru", "buffer_waves", "Волны");
            Add("ru", "buffer_map", "Места на карте");
            Add("ru", "buffer_switched", "{0}, элементов: {1}");
            Add("ru", "buffer_switched_one", "{0}, 1 элемент");
            Add("ru", "buffer_none", "Нечего просматривать");
            Add("ru", "verbosity_short", "Краткие описания. Подробности в буферах просмотра.");
            Add("ru", "verbosity_verbose", "Полные описания");

            // Polish
            Add("pl", "buffer_events", "Zdarzenia");
            Add("pl", "buffer_details", "Szczegoly");
            Add("pl", "buffer_hand", "Reka");
            Add("pl", "buffer_board", "Plansza");
            Add("pl", "buffer_resources", "Zasoby");
            Add("pl", "buffer_waves", "Fale");
            Add("pl", "buffer_map", "Miejsca na mapie");
            Add("pl", "buffer_switched", "{0}, elementow: {1}");
            Add("pl", "buffer_switched_one", "{0}, 1 element");
            Add("pl", "buffer_none", "Nie ma nic do przejrzenia");
            Add("pl", "verbosity_short", "Krotkie opisy. Szczegoly sa w buforach przegladu.");
            Add("pl", "verbosity_verbose", "Pelne opisy");

            // Turkish
            Add("tr", "buffer_events", "Olaylar");
            Add("tr", "buffer_details", "Ayrintilar");
            Add("tr", "buffer_hand", "El");
            Add("tr", "buffer_board", "Saha");
            Add("tr", "buffer_resources", "Kaynaklar");
            Add("tr", "buffer_waves", "Dalgalar");
            Add("tr", "buffer_map", "Harita konumlari");
            Add("tr", "buffer_switched", "{0}, {1} oge");
            Add("tr", "buffer_switched_one", "{0}, 1 oge");
            Add("tr", "buffer_none", "Incelenecek bir sey yok");
            Add("tr", "verbosity_short", "Kisa aciklamalar. Ayrintilar inceleme arabelleklerinde.");
            Add("tr", "verbosity_verbose", "Tam aciklamalar");

            // Buffer hint appended to every screen's F1 help
            Add("en", "help_buffers", "Control plus up or down steps through a review buffer; control plus left or right switches buffers. V toggles short or full descriptions.");
            Add("de", "help_buffers", "Strg plus Hoch oder Runter blaettert durch einen Lesepuffer; Strg plus Links oder Rechts wechselt den Puffer. V schaltet zwischen kurzen und ausfuehrlichen Beschreibungen um.");
            Add("fr", "help_buffers", "Ctrl plus haut ou bas parcourt un tampon de lecture; Ctrl plus gauche ou droite change de tampon. V bascule entre descriptions courtes et completes.");
            Add("es", "help_buffers", "Ctrl mas arriba o abajo recorre un bufer de revision; Ctrl mas izquierda o derecha cambia de bufer. V alterna descripciones cortas o completas.");
            Add("ja", "help_buffers", "Ctrl+上下でレビューバッファ内を移動、Ctrl+左右でバッファ切替。Vで説明の長さを切替。");
            Add("ko", "help_buffers", "Ctrl+위아래로 검토 버퍼 이동, Ctrl+좌우로 버퍼 전환. V로 설명 길이 전환.");
            Add("zh-Hans", "help_buffers", "Ctrl加上下键在查看缓冲区中移动，Ctrl加左右键切换缓冲区。V键切换描述长短。");
            Add("zh-Hant", "help_buffers", "Ctrl加上下鍵在檢視緩衝區中移動，Ctrl加左右鍵切換緩衝區。V鍵切換描述長短。");
            Add("it", "help_buffers", "Ctrl piu su o giu scorre un buffer di revisione; Ctrl piu sinistra o destra cambia buffer. V alterna descrizioni brevi o complete.");
            Add("pt", "help_buffers", "Ctrl mais cima ou baixo percorre um buffer de revisao; Ctrl mais esquerda ou direita troca de buffer. V alterna descricoes curtas ou completas.");
            Add("ru", "help_buffers", "Ctrl со стрелками вверх или вниз перемещает по буферу просмотра; Ctrl влево или вправо переключает буферы. V переключает краткие или полные описания.");
            Add("pl", "help_buffers", "Ctrl plus gora lub dol przewija bufor przegladu; Ctrl plus lewo lub prawo zmienia bufor. V przelacza krotkie lub pelne opisy.");
            Add("tr", "help_buffers", "Ctrl arti yukari veya asagi bir inceleme arabelleginde gezinir; Ctrl arti sol veya sag arabellek degistirir. V kisa veya tam aciklamalari degistirir.");
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

            // CampaignEnd score screen (win / defeat / vanquished run summary)
            Add("en", "campaignend_win", "Victory!");
            Add("en", "campaignend_defeat", "Defeated.");
            Add("en", "campaignend_vanquished", "Vanquished!");
            Add("en", "campaignend_time", "Time");
            Add("en", "campaignend_battles", "Battles won");
            Add("en", "campaignend_blings", "Blings");
            Add("en", "campaignend_score", "Your score: {0}.");
            Add("en", "campaignend_town_progress", "Town progress: {0}.");
            Add("en", "help_campaignend", "Run summary shown when a journey ends: your result, run stats, town progress, and final score. Arrow keys reach Back To Town and Scores, Enter selects. Ctrl+Up replays the summary.");

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
            Add("en", "building_overlay_hint", "Arrow keys move between banners, Enter opens one, Escape leaves, I reads again.");
            Add("en", "building_closed", "Closed.");
            Add("en", "building_back", "Back.");
            Add("en", "overlay_item", "{0}, {1} of {2}");
            Add("en", "tribe_banner", "Tribe banner");
            Add("en", "tribe_unlock_intro", "Complete this challenge to unlock the next tribe:");
            Add("en", "pethut_pets", "{0} of {1} pets unlocked: {2}");
            Add("en", "pethut_unlock_intro", "Complete this challenge to unlock the next pet:");
            Add("en", "pethut_hint", "Arrow keys move between your pets, Escape leaves, I reads again.");
            Add("en", "overlay_browse", "{0} items. Use the arrow keys to browse them.");
            Add("en", "overlay_position", "{0} of {1}");
            Add("en", "challenge_stone", "Challenge");
            Add("en", "challenge_hidden", "{0}, hidden challenge");
            Add("en", "shrine_summary", "{0} incomplete, {1} completed.");
            Add("en", "shrine_hint", "Up and down switch between incomplete and completed, left and right browse.");
            Add("en", "shrine_row_incomplete", "Incomplete challenges");
            Add("en", "shrine_row_completed", "Completed challenges");
            Add("en", "shrine_none_incomplete", "No incomplete challenges.");
            Add("en", "shrine_none_completed", "No completed challenges.");
            Add("en", "gate_continue_tutorial", "Your tutorial journey is in progress. Press Enter to continue it");
            Add("en", "gate_start_tutorial", "Starts your first journey. The game will offer you the tutorial");
            Add("en", "gate_continue_run", "Your journey is in progress. Press Enter to continue it");
            Add("en", "gate_start_run", "Starts a new journey");
            Add("en", "help_town", "Town. Your base between journeys. Buildings unlock new cards and challenges. Arrow keys move between buildings, I reads what a building does, Enter opens it. The Gate starts or continues your journey.");

            // Daily Voyage balloon
            Add("en", "balloon_start_run", "Starts the daily run: a fixed deck and modifiers, scored on the leaderboard. Enter opens today's voyage");
            Add("en", "balloon_continue_run", "Your daily run is in progress. Press Enter to continue it");
            Add("en", "balloon_deck", "Fixed deck of {0} cards: {1}");
            Add("en", "balloon_modifiers", "{0} modifiers: {1}");
            Add("en", "balloon_loading", "Daily voyage. Loading today's run.");
            Add("en", "balloon_play_desc", "Starts today's daily run");
            Add("en", "balloon_scores_desc", "Opens the leaderboard for today's run");
            Add("en", "balloon_buttons_hint", "Left and right move between Let's Go and Scores, Enter chooses, I reads this again, Escape leaves");

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
            Add("en", "map_hint", "Left and right arrows move along the path, Enter travels. M reads the whole map, I reads details, G reads gold, P opens the inventory.");
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
            Add("en", "help_map", "Campaign map. Your journey is a path of locations. Left and right arrows move between locations. Enter travels to an available location. Up and down arrows reach your deck piles and other controls. M reads the whole map, I reads details of the focused location including enemies, G reads your gold. P opens your inventory with your deck and charms.");

            // Inventory overlay (deckpack): deck, reserve, charms, crowns
            Add("en", "deckpack_unavailable", "No inventory on this screen.");
            Add("en", "deckpack_blocked", "The inventory can't be opened right now.");
            Add("en", "deckpack_open", "Inventory open.");
            Add("en", "deckpack_closed", "Inventory closed.");
            Add("en", "deckpack_part_deck", "{0} in the deck");
            Add("en", "deckpack_part_reserve", "{0} in reserve");
            Add("en", "deckpack_part_charms", "{0} charms");
            Add("en", "deckpack_part_charm_one", "1 charm");
            Add("en", "deckpack_part_crowns", "{0} crowns");
            Add("en", "deckpack_part_crown_one", "1 crown");
            Add("en", "deckpack_hint", "Up and down arrows switch groups, left and right move within a group. Enter on a charm picks it up, Enter on a card opens its options. P or Escape closes. F1 for full help.");
            Add("en", "deckpack_group_deck", "Deck, {0} cards");
            Add("en", "deckpack_group_deck_one", "Deck, 1 card");
            Add("en", "deckpack_group_reserve", "Reserve, {0} cards");
            Add("en", "deckpack_group_reserve_one", "Reserve, 1 card");
            Add("en", "deckpack_group_charms", "Charms, {0}");
            Add("en", "deckpack_group_crowns", "Crowns, {0}");
            Add("en", "deckpack_group_controls", "Buttons");
            Add("en", "deckpack_pickup", "{0} picked up. {1} cards can take it. Arrows move between them, Enter attaches permanently, Escape puts it back.");
            Add("en", "deckpack_pickup_one", "{0} picked up. One card can take it. Enter attaches permanently, Escape puts it back.");
            Add("en", "deckpack_pickup_none", "{0}: no card can take this right now. Put back.");
            Add("en", "deckpack_battle_blocked", "Charms can't be attached during battle.");
            Add("en", "deckpack_returned", "{0} put back.");
            Add("en", "deckpack_applying", "Attaching {0} to {1}.");
            Add("en", "deckpack_applied", "{0} attached.");
            Add("en", "deckpack_not_eligible", "This card can't take {0}.");
            Add("en", "deckpack_target_slots", "{0} of {1} charm slots used");
            Add("en", "deckpack_menu_open", "Options for {0}: {1}. Left and right arrows choose, Enter activates, Escape closes.");
            Add("en", "deckpack_menu_closed", "Options closed.");
            Add("en", "deckpack_option_rename", "Rename");
            Add("en", "deckpack_option_take_crown", "Take crown");
            Add("en", "deckpack_option_move_reserve", "Move to reserve");
            Add("en", "deckpack_option_move_deck", "Move to deck");
            Add("en", "deckpack_moved_reserve", "{0} moved to the reserve.");
            Add("en", "deckpack_moved_deck", "{0} moved to the deck.");
            Add("en", "deckpack_crown_taken", "Crown removed from {0} and returned to the inventory.");
            Add("en", "deckpack_card_blocked", "Card options are not available right now.");
            Add("en", "help_deckpack", "Inventory. Your deck, reserve cards, and collected charms and crowns. Up and down arrows switch groups: deck, reserve, charms, crowns, and buttons. Left and right arrows move within a group. Enter on a charm or crown picks it up: arrows then move between the cards that can take it, Enter attaches it permanently, Escape puts it back. Enter on a card opens its options, like moving it between deck and reserve. I inspects the focused card. P or Escape closes the inventory.");

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
            Add("en", "help_battle", "Battle. Up and down arrows switch groups: hand, your board, enemy board, bell and piles. Left and right arrows move within a group. Enter on a hand card picks it up, arrows choose a target, Enter places it. Enter on one of your units on the board picks it up to move it: a free slot moves it, an occupied slot swaps or shoves, the recall zone takes it off the board. Moving and recalling are free actions that do not end your turn. Escape puts a picked-up card back. I inspects the focused card, Escape closes the inspect view. Playing a card or ringing the bell ends your turn. Readout keys: H hand, B board, W waves, R bell, T turn, G gold, M modifier bells. P opens your inventory. Press O for the game menu with settings.");
            Add("en", "battle_unit_picked_up", "{0} picked up from the board.");
            Add("en", "battle_move_hint", "Arrow keys choose a destination slot or the recall zone, Enter confirms, Escape cancels.");
            Add("en", "tutorial_drag_hint", "To select and place: press Enter on the card, choose the destination with the arrow keys, then press Enter again.");
            Add("en", "tutorial_drag_hint_infront", "To do this: press Enter on the card, use the arrow keys to choose the unit you want it in front of, then press Enter — your card takes that spot and pushes the unit back.");
            Add("en", "tutorial_inspect_hint", "You can also hold Control and press the up arrow to step through everything about the focused card, one detail at a time. Control plus left or right switches to other review buffers.");
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
            Add("en", "help_panel_hint", "Left and right arrows choose a button, Enter presses it, Escape goes back.");
            Add("en", "help_panel_no_back", "This prompt needs an answer. Use left and right arrows, then Enter.");
            Add("en", "help_panel_closed", "Closed.");
            Add("en", "help_event", "Event screen. A story event on your journey; its title and story are read as they appear. Arrow keys move between items, Enter activates. I inspects the focused card the way right-click does for sighted players, Escape closes the inspect view. P opens your inventory.");

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
            Add("de", "campaignend_win", "Sieg!");
            Add("de", "campaignend_defeat", "Besiegt.");
            Add("de", "campaignend_vanquished", "Bezwungen!");
            Add("de", "campaignend_time", "Zeit");
            Add("de", "campaignend_battles", "Gewonnene Kaempfe");
            Add("de", "campaignend_blings", "Blings");
            Add("de", "campaignend_score", "Deine Punktzahl: {0}.");
            Add("de", "campaignend_town_progress", "Stadtfortschritt: {0}.");
            Add("de", "help_campaignend", "Zusammenfassung am Ende einer Reise: Ergebnis, Laufstatistiken, Stadtfortschritt und Endpunktzahl. Pfeiltasten erreichen Zurueck zur Stadt und Punkte, Enter waehlt. Strg+Hoch wiederholt die Zusammenfassung.");
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

            // Daily Voyage balloon
            Add("de", "balloon_start_run", "Startet die taegliche Reise: ein festes Deck und Modifikatoren, gewertet in der Bestenliste. Enter oeffnet die heutige Reise");
            Add("de", "balloon_continue_run", "Deine taegliche Reise laeuft. Enter setzt sie fort");
            Add("de", "balloon_deck", "Festes Deck mit {0} Karten: {1}");
            Add("de", "balloon_modifiers", "{0} Modifikatoren: {1}");
            Add("de", "balloon_loading", "Taegliche Reise. Heutige Reise wird geladen.");
            Add("de", "balloon_play_desc", "Startet die heutige taegliche Reise");
            Add("de", "balloon_scores_desc", "Oeffnet die Bestenliste der heutigen Reise");
            Add("de", "balloon_buttons_hint", "Links und rechts wechseln zwischen Los geht's und Bestenliste, Enter waehlt, I liest erneut vor, Escape verlaesst");
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
            Add("de", "map_hint", "Pfeiltasten links und rechts bewegen sich auf dem Pfad, Enter reist. M liest die ganze Karte, I liest Details, G liest Gold, P oeffnet das Inventar.");
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
            Add("de", "help_map", "Reisekarte. Deine Reise ist ein Pfad aus Orten. Pfeiltasten links und rechts wechseln zwischen Orten. Enter reist zu einem verfuegbaren Ort. Hoch und runter erreichen Kartenstapel und weitere Elemente. M liest die ganze Karte, I liest Details samt Gegnern, G liest dein Gold. P oeffnet dein Inventar mit Deck und Talismanen.");

            // Inventar-Overlay (Deckpack): Deck, Reserve, Talismane, Kronen
            Add("de", "deckpack_unavailable", "Kein Inventar auf diesem Bildschirm.");
            Add("de", "deckpack_blocked", "Das Inventar kann gerade nicht geoeffnet werden.");
            Add("de", "deckpack_open", "Inventar geoeffnet.");
            Add("de", "deckpack_closed", "Inventar geschlossen.");
            Add("de", "deckpack_part_deck", "{0} im Deck");
            Add("de", "deckpack_part_reserve", "{0} in der Reserve");
            Add("de", "deckpack_part_charms", "{0} Talismane");
            Add("de", "deckpack_part_charm_one", "1 Talisman");
            Add("de", "deckpack_part_crowns", "{0} Kronen");
            Add("de", "deckpack_part_crown_one", "1 Krone");
            Add("de", "deckpack_hint", "Hoch und runter wechseln die Gruppen, links und rechts bewegen sich innerhalb. Enter auf einem Talisman nimmt ihn auf, Enter auf einer Karte oeffnet ihre Optionen. P oder Escape schliesst. F1 fuer die volle Hilfe.");
            Add("de", "deckpack_group_deck", "Deck, {0} Karten");
            Add("de", "deckpack_group_deck_one", "Deck, 1 Karte");
            Add("de", "deckpack_group_reserve", "Reserve, {0} Karten");
            Add("de", "deckpack_group_reserve_one", "Reserve, 1 Karte");
            Add("de", "deckpack_group_charms", "Talismane, {0}");
            Add("de", "deckpack_group_crowns", "Kronen, {0}");
            Add("de", "deckpack_group_controls", "Schaltflaechen");
            Add("de", "deckpack_pickup", "{0} aufgenommen. {1} Karten koennen ihn tragen. Pfeiltasten wechseln zwischen ihnen, Enter befestigt dauerhaft, Escape legt zurueck.");
            Add("de", "deckpack_pickup_one", "{0} aufgenommen. Eine Karte kann ihn tragen. Enter befestigt dauerhaft, Escape legt zurueck.");
            Add("de", "deckpack_pickup_none", "{0}: keine Karte kann das gerade tragen. Zurueckgelegt.");
            Add("de", "deckpack_battle_blocked", "Talismane koennen im Kampf nicht angebracht werden.");
            Add("de", "deckpack_returned", "{0} zurueckgelegt.");
            Add("de", "deckpack_applying", "{0} wird an {1} angebracht.");
            Add("de", "deckpack_applied", "{0} angebracht.");
            Add("de", "deckpack_not_eligible", "Diese Karte kann {0} nicht tragen.");
            Add("de", "deckpack_target_slots", "{0} von {1} Talisman-Plaetzen belegt");
            Add("de", "deckpack_menu_open", "Optionen fuer {0}: {1}. Links und rechts waehlen, Enter aktiviert, Escape schliesst.");
            Add("de", "deckpack_menu_closed", "Optionen geschlossen.");
            Add("de", "deckpack_option_rename", "Umbenennen");
            Add("de", "deckpack_option_take_crown", "Krone abnehmen");
            Add("de", "deckpack_option_move_reserve", "In die Reserve");
            Add("de", "deckpack_option_move_deck", "Ins Deck");
            Add("de", "deckpack_moved_reserve", "{0} in die Reserve verschoben.");
            Add("de", "deckpack_moved_deck", "{0} ins Deck verschoben.");
            Add("de", "deckpack_crown_taken", "Krone von {0} abgenommen und ins Inventar gelegt.");
            Add("de", "deckpack_card_blocked", "Kartenoptionen sind gerade nicht verfuegbar.");
            Add("de", "help_deckpack", "Inventar. Dein Deck, Reservekarten und gesammelte Talismane und Kronen. Hoch und runter wechseln die Gruppen: Deck, Reserve, Talismane, Kronen und Schaltflaechen. Links und rechts bewegen sich innerhalb einer Gruppe. Enter auf einem Talisman oder einer Krone nimmt sie auf: Pfeiltasten wechseln dann zwischen den Karten, die sie tragen koennen, Enter befestigt dauerhaft, Escape legt zurueck. Enter auf einer Karte oeffnet ihre Optionen, etwa zwischen Deck und Reserve verschieben. I untersucht die fokussierte Karte. P oder Escape schliesst das Inventar.");
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
            Add("de", "help_battle", "Kampf. Hoch und runter wechseln die Gruppen: Hand, dein Feld, Gegnerfeld, Glocke und Stapel. Links und rechts bewegen sich innerhalb einer Gruppe. Enter auf einer Handkarte nimmt sie auf, Pfeiltasten waehlen ein Ziel, Enter legt ab. Enter auf einer deiner Einheiten auf dem Feld nimmt sie zum Bewegen auf: ein freier Platz bewegt sie, ein besetzter Platz tauscht oder schiebt, die Rueckrufzone nimmt sie vom Feld. Bewegen und Zurueckrufen sind freie Aktionen und beenden deinen Zug nicht. Escape legt eine aufgenommene Karte zurueck. I untersucht die fokussierte Karte, Escape schliesst die Untersuchung. Eine Karte spielen oder die Glocke laeuten beendet deinen Zug. Vorlesetasten: H Hand, B Feld, W Wellen, R Glocke, T Runde, G Gold, M Modifikator-Glocken. P oeffnet dein Inventar. O oeffnet das Spielmenue mit Einstellungen.");
            Add("de", "battle_unit_picked_up", "{0} vom Feld aufgenommen.");
            Add("de", "battle_move_hint", "Pfeiltasten waehlen einen Zielplatz oder die Rueckrufzone, Enter bestaetigt, Escape bricht ab.");
            Add("de", "tutorial_drag_hint", "Zum Auswaehlen und Ablegen: Enter auf der Karte druecken, das Ziel mit den Pfeiltasten waehlen, dann erneut Enter druecken.");
            Add("de", "tutorial_inspect_hint", "Du kannst auch Strg gedrueckt halten und Hoch druecken, um alle Details der ausgewaehlten Karte einzeln zu hoeren. Strg plus Links oder Rechts wechselt zu anderen Lesepuffern.");
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
            Add("de", "help_panel_hint", "Pfeiltasten links und rechts waehlen eine Schaltflaeche, Enter drueckt sie, Escape geht zurueck.");
            Add("de", "help_panel_no_back", "Diese Abfrage braucht eine Antwort. Links und rechts waehlen, dann Enter.");
            Add("de", "help_panel_closed", "Geschlossen.");
            Add("de", "help_event", "Ereignis-Bildschirm. Ein Story-Ereignis auf deiner Reise; Titel und Text werden vorgelesen, sobald sie erscheinen. Pfeiltasten wechseln zwischen Elementen, Enter aktiviert. I untersucht die fokussierte Karte, so wie Rechtsklick fuer sehende Spieler, Escape schliesst die Untersuchung. P oeffnet dein Inventar.");

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
            Add("es", "campaignend_win", "Victoria!");
            Add("es", "campaignend_defeat", "Derrotado.");
            Add("es", "campaignend_vanquished", "Victoria total!");
            Add("es", "campaignend_time", "Tiempo");
            Add("es", "campaignend_battles", "Batallas ganadas");
            Add("es", "campaignend_blings", "Blings");
            Add("es", "campaignend_score", "Tu puntuacion: {0}.");
            Add("es", "campaignend_town_progress", "Progreso del pueblo: {0}.");
            Add("es", "help_campaignend", "Resumen al terminar un viaje: resultado, estadisticas de la partida, progreso del pueblo y puntuacion final. Las flechas llegan a Volver al pueblo y Puntuaciones, Enter selecciona. Ctrl+Arriba repite el resumen.");
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

            // Daily Voyage balloon
            Add("es", "balloon_start_run", "Comienza el viaje diario: un mazo fijo y modificadores, puntuado en la clasificacion. Enter abre el viaje de hoy");
            Add("es", "balloon_continue_run", "Tu viaje diario esta en curso. Pulsa Enter para continuarlo");
            Add("es", "balloon_deck", "Mazo fijo de {0} cartas: {1}");
            Add("es", "balloon_modifiers", "{0} modificadores: {1}");
            Add("es", "balloon_loading", "Viaje diario. Cargando el viaje de hoy.");
            Add("es", "balloon_play_desc", "Comienza el viaje diario de hoy");
            Add("es", "balloon_scores_desc", "Abre la clasificacion del viaje de hoy");
            Add("es", "balloon_buttons_hint", "Izquierda y derecha cambian entre Vamos y Clasificacion, Enter elige, I lo lee otra vez, Escape sale");
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
            Add("es", "map_hint", "Flechas izquierda y derecha recorren el camino, Enter viaja. M lee todo el mapa, I lee detalles, G lee el oro, P abre el inventario.");
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
            Add("es", "help_map", "Mapa del viaje. Tu viaje es un camino de lugares. Flechas izquierda y derecha cambian de lugar. Enter viaja a un lugar disponible. Arriba y abajo alcanzan los mazos y otros controles. M lee todo el mapa, I lee detalles del lugar seleccionado incluidos enemigos, G lee tu oro. P abre tu inventario con el mazo y los amuletos.");

            // Inventario (deckpack): mazo, reserva, amuletos, coronas
            Add("es", "deckpack_unavailable", "No hay inventario en esta pantalla.");
            Add("es", "deckpack_blocked", "El inventario no se puede abrir ahora.");
            Add("es", "deckpack_open", "Inventario abierto.");
            Add("es", "deckpack_closed", "Inventario cerrado.");
            Add("es", "deckpack_part_deck", "{0} en el mazo");
            Add("es", "deckpack_part_reserve", "{0} en la reserva");
            Add("es", "deckpack_part_charms", "{0} amuletos");
            Add("es", "deckpack_part_charm_one", "1 amuleto");
            Add("es", "deckpack_part_crowns", "{0} coronas");
            Add("es", "deckpack_part_crown_one", "1 corona");
            Add("es", "deckpack_hint", "Arriba y abajo cambian de grupo, izquierda y derecha se mueven dentro. Enter en un amuleto lo coge, Enter en una carta abre sus opciones. P o Escape cierra. F1 para la ayuda completa.");
            Add("es", "deckpack_group_deck", "Mazo, {0} cartas");
            Add("es", "deckpack_group_deck_one", "Mazo, 1 carta");
            Add("es", "deckpack_group_reserve", "Reserva, {0} cartas");
            Add("es", "deckpack_group_reserve_one", "Reserva, 1 carta");
            Add("es", "deckpack_group_charms", "Amuletos, {0}");
            Add("es", "deckpack_group_crowns", "Coronas, {0}");
            Add("es", "deckpack_group_controls", "Botones");
            Add("es", "deckpack_pickup", "{0} cogido. {1} cartas pueden llevarlo. Las flechas cambian entre ellas, Enter lo fija de forma permanente, Escape lo devuelve.");
            Add("es", "deckpack_pickup_one", "{0} cogido. Una carta puede llevarlo. Enter lo fija de forma permanente, Escape lo devuelve.");
            Add("es", "deckpack_pickup_none", "{0}: ninguna carta puede llevarlo ahora. Devuelto.");
            Add("es", "deckpack_battle_blocked", "Los amuletos no se pueden fijar durante la batalla.");
            Add("es", "deckpack_returned", "{0} devuelto.");
            Add("es", "deckpack_applying", "Fijando {0} a {1}.");
            Add("es", "deckpack_applied", "{0} fijado.");
            Add("es", "deckpack_not_eligible", "Esta carta no puede llevar {0}.");
            Add("es", "deckpack_target_slots", "{0} de {1} huecos de amuleto usados");
            Add("es", "deckpack_menu_open", "Opciones de {0}: {1}. Izquierda y derecha eligen, Enter activa, Escape cierra.");
            Add("es", "deckpack_menu_closed", "Opciones cerradas.");
            Add("es", "deckpack_option_rename", "Renombrar");
            Add("es", "deckpack_option_take_crown", "Quitar corona");
            Add("es", "deckpack_option_move_reserve", "Mover a la reserva");
            Add("es", "deckpack_option_move_deck", "Mover al mazo");
            Add("es", "deckpack_moved_reserve", "{0} movida a la reserva.");
            Add("es", "deckpack_moved_deck", "{0} movida al mazo.");
            Add("es", "deckpack_crown_taken", "Corona quitada de {0} y devuelta al inventario.");
            Add("es", "deckpack_card_blocked", "Las opciones de carta no estan disponibles ahora.");
            Add("es", "help_deckpack", "Inventario. Tu mazo, cartas de reserva y los amuletos y coronas conseguidos. Arriba y abajo cambian de grupo: mazo, reserva, amuletos, coronas y botones. Izquierda y derecha se mueven dentro del grupo. Enter en un amuleto o corona lo coge: las flechas cambian entre las cartas que pueden llevarlo, Enter lo fija de forma permanente, Escape lo devuelve. Enter en una carta abre sus opciones, como moverla entre mazo y reserva. I inspecciona la carta enfocada. P o Escape cierra el inventario.");
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
            Add("es", "help_battle", "Batalla. Arriba y abajo cambian de grupo: mano, tu tablero, tablero enemigo, campana y pilas. Izquierda y derecha se mueven dentro del grupo. Enter en una carta de la mano la coge, las flechas eligen objetivo, Enter la coloca. Enter en una de tus unidades del tablero la coge para moverla: una casilla libre la mueve, una ocupada intercambia o empuja, la zona de retirada la saca del tablero. Mover y retirar son acciones gratuitas que no terminan tu turno. Escape devuelve la carta cogida. I inspecciona la carta enfocada, Escape cierra la inspeccion. Jugar una carta o tocar la campana termina tu turno. Teclas de lectura: H mano, B tablero, W oleadas, R campana, T turno, G oro, M campanas de modificador. P abre tu inventario. Pulsa O para el menu del juego con los ajustes.");
            Add("es", "battle_unit_picked_up", "{0} levantada del tablero.");
            Add("es", "battle_move_hint", "Las flechas eligen una casilla de destino o la zona de retirada, Enter confirma, Escape cancela.");
            Add("es", "tutorial_drag_hint", "Para seleccionar y colocar: pulsa Enter sobre la carta, elige el destino con las flechas y pulsa Enter otra vez.");
            Add("es", "tutorial_inspect_hint", "Tambien puedes mantener Ctrl y pulsar arriba para recorrer todos los detalles de la carta seleccionada, uno por uno. Ctrl mas izquierda o derecha cambia a otros buferes de revision.");
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
            Add("es", "help_panel_hint", "Las flechas izquierda y derecha eligen un boton, Enter lo pulsa, Escape vuelve atras.");
            Add("es", "help_panel_no_back", "Este aviso necesita una respuesta. Usa izquierda y derecha, luego Enter.");
            Add("es", "help_panel_closed", "Cerrado.");
            Add("es", "help_event", "Pantalla de evento. Un evento de historia en tu viaje; su titulo y texto se leen cuando aparecen. Las flechas mueven entre elementos, Enter activa. I inspecciona la carta enfocada, como el clic derecho para jugadores videntes, Escape cierra la inspeccion. P abre tu inventario.");

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
            Add("fr", "campaignend_win", "Victoire!");
            Add("fr", "campaignend_defeat", "Vaincu.");
            Add("fr", "campaignend_vanquished", "Triomphe!");
            Add("fr", "campaignend_time", "Temps");
            Add("fr", "campaignend_battles", "Batailles gagnees");
            Add("fr", "campaignend_blings", "Blings");
            Add("fr", "campaignend_score", "Votre score: {0}.");
            Add("fr", "campaignend_town_progress", "Progression du village: {0}.");
            Add("fr", "help_campaignend", "Resume de fin de partie: resultat, statistiques de la partie, progression du village et score final. Les fleches atteignent Retour au village et Scores, Entree valide. Ctrl+Haut repete le resume.");
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

            // Daily Voyage balloon
            Add("fr", "balloon_start_run", "Commence le voyage quotidien: un deck fixe et des modificateurs, classe au tableau des scores. Entree ouvre le voyage du jour");
            Add("fr", "balloon_continue_run", "Votre voyage quotidien est en cours. Appuyez sur Entree pour le continuer");
            Add("fr", "balloon_deck", "Deck fixe de {0} cartes: {1}");
            Add("fr", "balloon_modifiers", "{0} modificateurs: {1}");
            Add("fr", "balloon_loading", "Voyage quotidien. Chargement du voyage du jour.");
            Add("fr", "balloon_play_desc", "Commence le voyage quotidien du jour");
            Add("fr", "balloon_scores_desc", "Ouvre le tableau des scores du jour");
            Add("fr", "balloon_buttons_hint", "Gauche et droite alternent entre C'est parti et Scores, Entree choisit, I relit, Echap quitte");
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
            Add("fr", "map_hint", "Fleches gauche et droite parcourent le chemin, Entree voyage. M lit toute la carte, I lit les details, G lit l'or, P ouvre l'inventaire.");
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
            Add("fr", "help_map", "Carte du voyage. Votre voyage est un chemin de lieux. Fleches gauche et droite changent de lieu. Entree voyage vers un lieu disponible. Haut et bas atteignent les piles de cartes et autres controles. M lit toute la carte, I lit les details du lieu selectionne, ennemis compris, G lit votre or. P ouvre votre inventaire avec le deck et les talismans.");

            // Inventaire (deckpack): deck, reserve, talismans, couronnes
            Add("fr", "deckpack_unavailable", "Pas d'inventaire sur cet ecran.");
            Add("fr", "deckpack_blocked", "L'inventaire ne peut pas etre ouvert maintenant.");
            Add("fr", "deckpack_open", "Inventaire ouvert.");
            Add("fr", "deckpack_closed", "Inventaire ferme.");
            Add("fr", "deckpack_part_deck", "{0} dans le deck");
            Add("fr", "deckpack_part_reserve", "{0} en reserve");
            Add("fr", "deckpack_part_charms", "{0} talismans");
            Add("fr", "deckpack_part_charm_one", "1 talisman");
            Add("fr", "deckpack_part_crowns", "{0} couronnes");
            Add("fr", "deckpack_part_crown_one", "1 couronne");
            Add("fr", "deckpack_hint", "Haut et bas changent de groupe, gauche et droite se deplacent a l'interieur. Entree sur un talisman le prend, Entree sur une carte ouvre ses options. P ou Echap ferme. F1 pour l'aide complete.");
            Add("fr", "deckpack_group_deck", "Deck, {0} cartes");
            Add("fr", "deckpack_group_deck_one", "Deck, 1 carte");
            Add("fr", "deckpack_group_reserve", "Reserve, {0} cartes");
            Add("fr", "deckpack_group_reserve_one", "Reserve, 1 carte");
            Add("fr", "deckpack_group_charms", "Talismans, {0}");
            Add("fr", "deckpack_group_crowns", "Couronnes, {0}");
            Add("fr", "deckpack_group_controls", "Boutons");
            Add("fr", "deckpack_pickup", "{0} pris. {1} cartes peuvent le porter. Les fleches passent de l'une a l'autre, Entree le fixe definitivement, Echap le repose.");
            Add("fr", "deckpack_pickup_one", "{0} pris. Une carte peut le porter. Entree le fixe definitivement, Echap le repose.");
            Add("fr", "deckpack_pickup_none", "{0}: aucune carte ne peut le porter pour le moment. Repose.");
            Add("fr", "deckpack_battle_blocked", "Les talismans ne peuvent pas etre fixes pendant la bataille.");
            Add("fr", "deckpack_returned", "{0} repose.");
            Add("fr", "deckpack_applying", "Fixation de {0} sur {1}.");
            Add("fr", "deckpack_applied", "{0} fixe.");
            Add("fr", "deckpack_not_eligible", "Cette carte ne peut pas porter {0}.");
            Add("fr", "deckpack_target_slots", "{0} emplacements de talisman sur {1} utilises");
            Add("fr", "deckpack_menu_open", "Options de {0}: {1}. Gauche et droite choisissent, Entree active, Echap ferme.");
            Add("fr", "deckpack_menu_closed", "Options fermees.");
            Add("fr", "deckpack_option_rename", "Renommer");
            Add("fr", "deckpack_option_take_crown", "Retirer la couronne");
            Add("fr", "deckpack_option_move_reserve", "Mettre en reserve");
            Add("fr", "deckpack_option_move_deck", "Mettre dans le deck");
            Add("fr", "deckpack_moved_reserve", "{0} mise en reserve.");
            Add("fr", "deckpack_moved_deck", "{0} mise dans le deck.");
            Add("fr", "deckpack_crown_taken", "Couronne retiree de {0} et rangee dans l'inventaire.");
            Add("fr", "deckpack_card_blocked", "Les options de carte ne sont pas disponibles maintenant.");
            Add("fr", "help_deckpack", "Inventaire. Votre deck, vos cartes en reserve et les talismans et couronnes obtenus. Haut et bas changent de groupe: deck, reserve, talismans, couronnes et boutons. Gauche et droite se deplacent dans un groupe. Entree sur un talisman ou une couronne le prend: les fleches passent alors entre les cartes qui peuvent le porter, Entree le fixe definitivement, Echap le repose. Entree sur une carte ouvre ses options, comme la deplacer entre deck et reserve. I inspecte la carte selectionnee. P ou Echap ferme l'inventaire.");
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
            Add("fr", "help_battle", "Bataille. Haut et bas changent de groupe: main, votre plateau, plateau ennemi, cloche et piles. Gauche et droite se deplacent dans le groupe. Entree sur une carte de la main la prend, les fleches choisissent une cible, Entree la pose. Entree sur une de vos unites du plateau la prend pour la deplacer: une case libre la deplace, une case occupee echange ou pousse, la zone de rappel la retire du plateau. Deplacer et rappeler sont des actions gratuites qui ne terminent pas votre tour. Echap repose une carte prise. I inspecte la carte selectionnee, Echap ferme l'inspection. Jouer une carte ou sonner la cloche termine votre tour. Touches de lecture: H main, B plateau, W vagues, R cloche, T tour, G or, M cloches de modificateur. P ouvre votre inventaire. Appuyez sur O pour le menu du jeu avec les reglages.");
            Add("fr", "battle_unit_picked_up", "{0} prise du plateau.");
            Add("fr", "battle_move_hint", "Les fleches choisissent une case de destination ou la zone de rappel, Entree confirme, Echap annule.");
            Add("fr", "tutorial_drag_hint", "Pour selectionner et placer: appuyez sur Entree sur la carte, choisissez la destination avec les fleches, puis appuyez encore sur Entree.");
            Add("fr", "tutorial_inspect_hint", "Vous pouvez aussi maintenir Ctrl et appuyer sur haut pour parcourir tous les details de la carte selectionnee, un par un. Ctrl plus gauche ou droite passe aux autres tampons de lecture.");
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
            Add("fr", "help_panel_hint", "Les fleches gauche et droite choisissent un bouton, Entree l'active, Echap revient en arriere.");
            Add("fr", "help_panel_no_back", "Cette invite demande une reponse. Utilisez gauche et droite, puis Entree.");
            Add("fr", "help_panel_closed", "Ferme.");
            Add("fr", "help_event", "Ecran d'evenement. Un evenement d'histoire sur votre voyage; son titre et son texte sont lus quand ils apparaissent. Les fleches naviguent entre les elements, Entree active. I inspecte la carte selectionnee, comme le clic droit pour les joueurs voyants, Echap ferme l'inspection. P ouvre votre inventaire.");
        }
    }
}
