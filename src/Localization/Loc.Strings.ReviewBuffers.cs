namespace WildfrostAccessibility
{
    /// <summary>
    /// Strings for the review buffers (Ctrl+arrows) and the V verbosity
    /// toggle, in all 13 registered languages.
    /// </summary>
    public static partial class Loc
    {
        /// <summary>
        /// Review buffer strings (Ctrl+arrows) and the V verbosity toggle,
        /// for all 13 registered languages, plus the buffer hint appended to
        /// every screen's F1 help.
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

    }
}
