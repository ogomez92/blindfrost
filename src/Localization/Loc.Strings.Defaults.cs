
namespace WildfrostAccessibility
{
    /// <summary>
    /// The default screen reader string table: the per-language base
    /// registrations, plus the calls that chain in the other string groups.
    /// </summary>
    public static partial class Loc
    {
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
            Add("en", "slot_your_row", "Your row {0}");
            Add("en", "slot_enemy_row", "Enemy row {0}");
            Add("en", "slot_position", "Slot {0}");
            Add("en", "slot_empty", "Empty");
            Add("en", "slot_occupied", "Occupied");
            Add("en", "slot_opposite", "facing {0}");
            Add("en", "slot_opposite_empty", "facing an empty slot");
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
            Add("de", "slot_your_row", "Deine Reihe {0}");
            Add("de", "slot_enemy_row", "Gegnerische Reihe {0}");
            Add("de", "slot_position", "Platz {0}");
            Add("de", "slot_empty", "Leer");
            Add("de", "slot_occupied", "Belegt");
            Add("de", "slot_opposite", "gegenueber {0}");
            Add("de", "slot_opposite_empty", "gegenueber einem leeren Platz");
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
            Add("fr", "slot_your_row", "Votre rangee {0}");
            Add("fr", "slot_enemy_row", "Rangee ennemie {0}");
            Add("fr", "slot_position", "Emplacement {0}");
            Add("fr", "slot_empty", "Vide");
            Add("fr", "slot_occupied", "Occupe");
            Add("fr", "slot_opposite", "face a {0}");
            Add("fr", "slot_opposite_empty", "face a un emplacement vide");
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
            Add("es", "slot_your_row", "Tu fila {0}");
            Add("es", "slot_enemy_row", "Fila enemiga {0}");
            Add("es", "slot_position", "Casilla {0}");
            Add("es", "slot_empty", "Vacio");
            Add("es", "slot_occupied", "Ocupado");
            Add("es", "slot_opposite", "frente a {0}");
            Add("es", "slot_opposite_empty", "frente a una casilla vacia");
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
            Add("ja", "slot_your_row", "味方{0}列目");
            Add("ja", "slot_enemy_row", "敵{0}列目");
            Add("ja", "slot_position", "{0}番");
            Add("ja", "slot_empty", "空き");
            Add("ja", "slot_occupied", "使用中");
            Add("ja", "slot_opposite", "正面は{0}");
            Add("ja", "slot_opposite_empty", "正面は空き");

            // Korean
            Add("ko", "mod_loaded", "Wildfrost 접근성 모드 로드됨. F1 도움말.");
            Add("ko", "mod_unloaded", "Wildfrost 접근성 모드 해제됨.");
            Add("ko", "help_text", "Wildfrost 접근성. F1: 도움말. F10: 디버그 모드 전환. 방향키: 탐색. Enter: 선택. Ctrl+방향키: 검토 버퍼. V: 설명 길이 전환.");
            Add("ko", "screen_main_menu", "메인 메뉴. 위아래 방향키로 탐색, Enter로 선택.");
            Add("ko", "tutorial_prompt", "튜토리얼: {0}");
            Add("ko", "slot_your_side", "아군 측");
            Add("ko", "slot_enemy_side", "적 측");
            Add("ko", "slot_row", "{0}행");
            Add("ko", "slot_your_row", "아군 {0}행");
            Add("ko", "slot_enemy_row", "적 {0}행");
            Add("ko", "slot_position", "{0}번 칸");
            Add("ko", "slot_empty", "비어있음");
            Add("ko", "slot_occupied", "점유됨");
            Add("ko", "slot_opposite", "맞은편 {0}");
            Add("ko", "slot_opposite_empty", "맞은편 비어있음");

            // Simplified Chinese
            Add("zh-Hans", "mod_loaded", "Wildfrost 无障碍已加载。按F1获取帮助。");
            Add("zh-Hans", "mod_unloaded", "Wildfrost 无障碍已卸载。");
            Add("zh-Hans", "help_text", "Wildfrost 无障碍。F1: 帮助。F10: 切换调试模式。方向键: 导航。回车: 确认。Ctrl加方向键: 查看缓冲区。V: 切换描述长短。");
            Add("zh-Hans", "screen_main_menu", "主菜单。上下方向键导航，回车键选择。");
            Add("zh-Hans", "tutorial_prompt", "教程: {0}");
            Add("zh-Hans", "slot_your_side", "己方");
            Add("zh-Hans", "slot_enemy_side", "敌方");
            Add("zh-Hans", "slot_row", "第{0}排");
            Add("zh-Hans", "slot_your_row", "己方第{0}排");
            Add("zh-Hans", "slot_enemy_row", "敌方第{0}排");
            Add("zh-Hans", "slot_position", "第{0}格");
            Add("zh-Hans", "slot_empty", "空");
            Add("zh-Hans", "slot_occupied", "已占用");
            Add("zh-Hans", "slot_opposite", "对面是{0}");
            Add("zh-Hans", "slot_opposite_empty", "对面为空");

            // Traditional Chinese
            Add("zh-Hant", "mod_loaded", "Wildfrost 無障礙已載入。按F1取得說明。");
            Add("zh-Hant", "mod_unloaded", "Wildfrost 無障礙已卸載。");
            Add("zh-Hant", "help_text", "Wildfrost 無障礙。F1: 說明。F10: 切換偵錯模式。方向鍵: 導航。Enter: 確認。Ctrl加方向鍵: 檢視緩衝區。V: 切換描述長短。");
            Add("zh-Hant", "screen_main_menu", "主選單。上下方向鍵導航，Enter鍵選擇。");
            Add("zh-Hant", "tutorial_prompt", "教學: {0}");
            Add("zh-Hant", "slot_your_side", "己方");
            Add("zh-Hant", "slot_enemy_side", "敵方");
            Add("zh-Hant", "slot_row", "第{0}排");
            Add("zh-Hant", "slot_your_row", "己方第{0}排");
            Add("zh-Hant", "slot_enemy_row", "敵方第{0}排");
            Add("zh-Hant", "slot_position", "第{0}格");
            Add("zh-Hant", "slot_empty", "空");
            Add("zh-Hant", "slot_occupied", "已佔用");
            Add("zh-Hant", "slot_opposite", "對面是{0}");
            Add("zh-Hant", "slot_opposite_empty", "對面為空");

            // Italian
            Add("it", "mod_loaded", "Accessibilita Wildfrost caricata. F1 per aiuto.");
            Add("it", "mod_unloaded", "Accessibilita Wildfrost disattivata.");
            Add("it", "help_text", "Accessibilita Wildfrost. F1: aiuto. F10: attiva/disattiva debug. Frecce: navigare. Invio: selezionare. Ctrl piu frecce: buffer di revisione. V: descrizioni brevi o complete.");
            Add("it", "screen_main_menu", "Menu principale. Frecce su e giu per navigare, Invio per selezionare.");
            Add("it", "tutorial_prompt", "Tutorial: {0}");
            Add("it", "slot_your_side", "Il tuo lato");
            Add("it", "slot_enemy_side", "Lato nemico");
            Add("it", "slot_row", "Riga {0}");
            Add("it", "slot_your_row", "La tua riga {0}");
            Add("it", "slot_enemy_row", "Riga nemica {0}");
            Add("it", "slot_position", "Posizione {0}");
            Add("it", "slot_empty", "Vuoto");
            Add("it", "slot_occupied", "Occupato");
            Add("it", "slot_opposite", "di fronte a {0}");
            Add("it", "slot_opposite_empty", "di fronte a una posizione vuota");

            // Portuguese
            Add("pt", "mod_loaded", "Acessibilidade Wildfrost carregada. F1 para ajuda.");
            Add("pt", "mod_unloaded", "Acessibilidade Wildfrost descarregada.");
            Add("pt", "help_text", "Acessibilidade Wildfrost. F1: ajuda. F10: alternar modo de depuracao. Setas: navegar. Enter: selecionar. Ctrl mais setas: buffers de revisao. V: descricoes curtas ou completas.");
            Add("pt", "screen_main_menu", "Menu principal. Setas para cima e para baixo para navegar, Enter para selecionar.");
            Add("pt", "tutorial_prompt", "Tutorial: {0}");
            Add("pt", "slot_your_side", "Seu lado");
            Add("pt", "slot_enemy_side", "Lado inimigo");
            Add("pt", "slot_row", "Fileira {0}");
            Add("pt", "slot_your_row", "Sua fileira {0}");
            Add("pt", "slot_enemy_row", "Fileira inimiga {0}");
            Add("pt", "slot_position", "Posicao {0}");
            Add("pt", "slot_empty", "Vazio");
            Add("pt", "slot_occupied", "Ocupado");
            Add("pt", "slot_opposite", "de frente para {0}");
            Add("pt", "slot_opposite_empty", "de frente para uma posicao vazia");

            // Russian
            Add("ru", "mod_loaded", "Доступность Wildfrost загружена. F1 для справки.");
            Add("ru", "mod_unloaded", "Доступность Wildfrost выгружена.");
            Add("ru", "help_text", "Доступность Wildfrost. F1: справка. F10: режим отладки. Стрелки: навигация. Enter: выбрать. Ctrl со стрелками: буферы просмотра. V: краткие или полные описания.");
            Add("ru", "screen_main_menu", "Главное меню. Стрелки вверх и вниз для навигации, Enter для выбора.");
            Add("ru", "tutorial_prompt", "Обучение: {0}");
            Add("ru", "slot_your_side", "Ваша сторона");
            Add("ru", "slot_enemy_side", "Сторона противника");
            Add("ru", "slot_row", "Ряд {0}");
            Add("ru", "slot_your_row", "Ваш ряд {0}");
            Add("ru", "slot_enemy_row", "Ряд противника {0}");
            Add("ru", "slot_position", "Позиция {0}");
            Add("ru", "slot_empty", "Пусто");
            Add("ru", "slot_occupied", "Занято");
            Add("ru", "slot_opposite", "напротив {0}");
            Add("ru", "slot_opposite_empty", "напротив пусто");

            // Polish
            Add("pl", "mod_loaded", "Dostepnosc Wildfrost zaladowana. F1 aby uzyskac pomoc.");
            Add("pl", "mod_unloaded", "Dostepnosc Wildfrost wylaczona.");
            Add("pl", "help_text", "Dostepnosc Wildfrost. F1: pomoc. F10: tryb debugowania. Strzalki: nawigacja. Enter: wybierz. Ctrl plus strzalki: bufory przegladu. V: krotkie lub pelne opisy.");
            Add("pl", "screen_main_menu", "Menu glowne. Strzalki gora i dol do nawigacji, Enter aby wybrac.");
            Add("pl", "tutorial_prompt", "Samouczek: {0}");
            Add("pl", "slot_your_side", "Twoja strona");
            Add("pl", "slot_enemy_side", "Strona wroga");
            Add("pl", "slot_row", "Rzad {0}");
            Add("pl", "slot_your_row", "Twoj rzad {0}");
            Add("pl", "slot_enemy_row", "Rzad wroga {0}");
            Add("pl", "slot_position", "Pozycja {0}");
            Add("pl", "slot_empty", "Puste");
            Add("pl", "slot_occupied", "Zajete");
            Add("pl", "slot_opposite", "naprzeciw {0}");
            Add("pl", "slot_opposite_empty", "naprzeciw puste pole");

            // Turkish
            Add("tr", "mod_loaded", "Wildfrost Erisilebilirlik yuklendi. Yardim icin F1.");
            Add("tr", "mod_unloaded", "Wildfrost Erisilebilirlik devre disi.");
            Add("tr", "help_text", "Wildfrost Erisilebilirlik. F1: yardim. F10: hata ayiklama modu. Ok tuslari: gezinme. Enter: sec. Ctrl arti ok tuslari: inceleme arabellekleri. V: kisa veya tam aciklamalar.");
            Add("tr", "screen_main_menu", "Ana menu. Yukari ve asagi ok tuslariyla gezinin, Enter ile secin.");
            Add("tr", "tutorial_prompt", "Egitim: {0}");
            Add("tr", "slot_your_side", "Sizin taraf");
            Add("tr", "slot_enemy_side", "Dusman tarafi");
            Add("tr", "slot_row", "Sira {0}");
            Add("tr", "slot_your_row", "Sizin sira {0}");
            Add("tr", "slot_enemy_row", "Dusman sirasi {0}");
            Add("tr", "slot_position", "Konum {0}");
            Add("tr", "slot_empty", "Bos");
            Add("tr", "slot_occupied", "Dolu");
            Add("tr", "slot_opposite", "karsisinda {0}");
            Add("tr", "slot_opposite_empty", "karsisinda bos bir yer");

            // More languages can be added as needed.
            // The game supports many more locales; they will fall back to English until translated.

            RegisterReviewBufferStrings();
            RegisterHandlerStrings();
            RegisterOverlayAndNarrationStrings();
        }

    }
}
