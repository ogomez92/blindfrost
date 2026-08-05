namespace WildfrostAccessibility
{
    /// <summary>
    /// Strings for the mod's own settings menu (F2). Unlike the per-screen
    /// tables, this one is translated into every language the mod knows: the
    /// settings menu is how a player picks their language in the first place,
    /// so it has to be understandable before the switch, not after it.
    ///
    /// House style in the string tables is accent-free Latin text ("Hauptmenue",
    /// "Accessibilite"); the language names in Loc.Endonyms are the exception
    /// and keep their real spelling, because they are names being read out and
    /// "Espanol" does not sound like "Español".
    /// </summary>
    public static partial class Loc
    {
        private static void RegisterSettingsStrings()
        {
            // English
            Add("en", "settings_opened", "Accessibility settings. Up and down arrows move between settings, left and right change the setting you are on, Escape closes.");
            Add("en", "settings_closed", "Settings closed.");
            Add("en", "settings_help", "Accessibility settings. Up and down: move between settings. Left and right: change the setting. Enter: next value. Escape or F2: close.");
            Add("en", "settings_first", "First setting.");
            Add("en", "settings_last", "Last setting.");
            Add("en", "settings_language", "Language");
            Add("en", "settings_language_auto", "Automatic");
            Add("en", "settings_language_partial", "partly translated, the rest is spoken in English");
            Add("en", "settings_detail", "Detail level");
            Add("en", "settings_detail_short", "Short, details wait in the buffers");
            Add("en", "settings_detail_full", "Full descriptions");
            Add("en", "settings_key_repeat", "Key repeat speed");
            Add("en", "settings_key_repeat_slow", "Slow");
            Add("en", "settings_key_repeat_normal", "Normal");
            Add("en", "settings_key_repeat_fast", "Fast");
            Add("en", "settings_debug", "Debug logging");
            Add("en", "settings_on", "On");
            Add("en", "settings_off", "Off");
            Add("en", "settings_hint", "Press F2 for accessibility settings.");

            // German
            Add("de", "settings_opened", "Barrierefreiheits-Einstellungen. Pfeiltasten hoch und runter wechseln die Einstellung, links und rechts aendern sie, Escape schliesst.");
            Add("de", "settings_closed", "Einstellungen geschlossen.");
            Add("de", "settings_help", "Barrierefreiheits-Einstellungen. Hoch und runter: Einstellung wechseln. Links und rechts: Wert aendern. Enter: naechster Wert. Escape oder F2: schliessen.");
            Add("de", "settings_first", "Erste Einstellung.");
            Add("de", "settings_last", "Letzte Einstellung.");
            Add("de", "settings_language", "Sprache");
            Add("de", "settings_language_auto", "Automatisch");
            Add("de", "settings_language_partial", "teilweise uebersetzt, der Rest wird auf Englisch gesprochen");
            Add("de", "settings_detail", "Detailgrad");
            Add("de", "settings_detail_short", "Kurz, Details warten in den Lesepuffern");
            Add("de", "settings_detail_full", "Vollstaendige Beschreibungen");
            Add("de", "settings_key_repeat", "Tastenwiederholung");
            Add("de", "settings_key_repeat_slow", "Langsam");
            Add("de", "settings_key_repeat_normal", "Normal");
            Add("de", "settings_key_repeat_fast", "Schnell");
            Add("de", "settings_debug", "Debug-Protokoll");
            Add("de", "settings_on", "An");
            Add("de", "settings_off", "Aus");
            Add("de", "settings_hint", "F2 fuer die Barrierefreiheits-Einstellungen.");

            // Spanish
            Add("es", "settings_opened", "Ajustes de accesibilidad. Flechas arriba y abajo para cambiar de ajuste, izquierda y derecha para modificarlo, Escape para cerrar.");
            Add("es", "settings_closed", "Ajustes cerrados.");
            Add("es", "settings_help", "Ajustes de accesibilidad. Arriba y abajo: cambiar de ajuste. Izquierda y derecha: modificar el ajuste. Enter: siguiente valor. Escape o F2: cerrar.");
            Add("es", "settings_first", "Primer ajuste.");
            Add("es", "settings_last", "Ultimo ajuste.");
            Add("es", "settings_language", "Idioma");
            Add("es", "settings_language_auto", "Automatico");
            Add("es", "settings_language_partial", "traduccion parcial, el resto se narra en ingles");
            Add("es", "settings_detail", "Nivel de detalle");
            Add("es", "settings_detail_short", "Breve, los detalles esperan en los buferes");
            Add("es", "settings_detail_full", "Descripciones completas");
            Add("es", "settings_key_repeat", "Velocidad de repeticion de teclas");
            Add("es", "settings_key_repeat_slow", "Lenta");
            Add("es", "settings_key_repeat_normal", "Normal");
            Add("es", "settings_key_repeat_fast", "Rapida");
            Add("es", "settings_debug", "Registro de depuracion");
            Add("es", "settings_on", "Activado");
            Add("es", "settings_off", "Desactivado");
            Add("es", "settings_hint", "Pulsa F2 para los ajustes de accesibilidad.");

            // French
            Add("fr", "settings_opened", "Parametres d'accessibilite. Fleches haut et bas pour changer de parametre, gauche et droite pour le modifier, Echap pour fermer.");
            Add("fr", "settings_closed", "Parametres fermes.");
            Add("fr", "settings_help", "Parametres d'accessibilite. Haut et bas: changer de parametre. Gauche et droite: modifier le parametre. Entree: valeur suivante. Echap ou F2: fermer.");
            Add("fr", "settings_first", "Premier parametre.");
            Add("fr", "settings_last", "Dernier parametre.");
            Add("fr", "settings_language", "Langue");
            Add("fr", "settings_language_auto", "Automatique");
            Add("fr", "settings_language_partial", "traduction partielle, le reste est lu en anglais");
            Add("fr", "settings_detail", "Niveau de detail");
            Add("fr", "settings_detail_short", "Court, les details attendent dans les tampons");
            Add("fr", "settings_detail_full", "Descriptions completes");
            Add("fr", "settings_key_repeat", "Vitesse de repetition des touches");
            Add("fr", "settings_key_repeat_slow", "Lente");
            Add("fr", "settings_key_repeat_normal", "Normale");
            Add("fr", "settings_key_repeat_fast", "Rapide");
            Add("fr", "settings_debug", "Journal de debogage");
            Add("fr", "settings_on", "Active");
            Add("fr", "settings_off", "Desactive");
            Add("fr", "settings_hint", "Appuyez sur F2 pour les parametres d'accessibilite.");

            // Italian
            Add("it", "settings_opened", "Impostazioni di accessibilita. Frecce su e giu per cambiare impostazione, sinistra e destra per modificarla, Escape per chiudere.");
            Add("it", "settings_closed", "Impostazioni chiuse.");
            Add("it", "settings_help", "Impostazioni di accessibilita. Su e giu: cambiare impostazione. Sinistra e destra: modificare l'impostazione. Invio: valore successivo. Escape o F2: chiudere.");
            Add("it", "settings_first", "Prima impostazione.");
            Add("it", "settings_last", "Ultima impostazione.");
            Add("it", "settings_language", "Lingua");
            Add("it", "settings_language_auto", "Automatica");
            Add("it", "settings_language_partial", "traduzione parziale, il resto viene letto in inglese");
            Add("it", "settings_detail", "Livello di dettaglio");
            Add("it", "settings_detail_short", "Breve, i dettagli restano nei buffer");
            Add("it", "settings_detail_full", "Descrizioni complete");
            Add("it", "settings_key_repeat", "Velocita di ripetizione dei tasti");
            Add("it", "settings_key_repeat_slow", "Lenta");
            Add("it", "settings_key_repeat_normal", "Normale");
            Add("it", "settings_key_repeat_fast", "Veloce");
            Add("it", "settings_debug", "Registro di debug");
            Add("it", "settings_on", "Attivo");
            Add("it", "settings_off", "Disattivo");
            Add("it", "settings_hint", "Premi F2 per le impostazioni di accessibilita.");

            // Portuguese
            Add("pt", "settings_opened", "Definicoes de acessibilidade. Setas para cima e para baixo mudam de definicao, esquerda e direita alteram-na, Escape fecha.");
            Add("pt", "settings_closed", "Definicoes fechadas.");
            Add("pt", "settings_help", "Definicoes de acessibilidade. Cima e baixo: mudar de definicao. Esquerda e direita: alterar a definicao. Enter: valor seguinte. Escape ou F2: fechar.");
            Add("pt", "settings_first", "Primeira definicao.");
            Add("pt", "settings_last", "Ultima definicao.");
            Add("pt", "settings_language", "Idioma");
            Add("pt", "settings_language_auto", "Automatico");
            Add("pt", "settings_language_partial", "traducao parcial, o resto e lido em ingles");
            Add("pt", "settings_detail", "Nivel de detalhe");
            Add("pt", "settings_detail_short", "Curto, os detalhes ficam nos buffers");
            Add("pt", "settings_detail_full", "Descricoes completas");
            Add("pt", "settings_key_repeat", "Velocidade de repeticao das teclas");
            Add("pt", "settings_key_repeat_slow", "Lenta");
            Add("pt", "settings_key_repeat_normal", "Normal");
            Add("pt", "settings_key_repeat_fast", "Rapida");
            Add("pt", "settings_debug", "Registo de depuracao");
            Add("pt", "settings_on", "Ligado");
            Add("pt", "settings_off", "Desligado");
            Add("pt", "settings_hint", "Prime F2 para as definicoes de acessibilidade.");

            // Russian
            Add("ru", "settings_opened", "Настройки доступности. Стрелки вверх и вниз переключают настройки, влево и вправо меняют значение, Escape закрывает.");
            Add("ru", "settings_closed", "Настройки закрыты.");
            Add("ru", "settings_help", "Настройки доступности. Вверх и вниз: переключить настройку. Влево и вправо: изменить значение. Enter: следующее значение. Escape или F2: закрыть.");
            Add("ru", "settings_first", "Первая настройка.");
            Add("ru", "settings_last", "Последняя настройка.");
            Add("ru", "settings_language", "Язык");
            Add("ru", "settings_language_auto", "Автоматически");
            Add("ru", "settings_language_partial", "частичный перевод, остальное озвучивается на английском");
            Add("ru", "settings_detail", "Уровень детализации");
            Add("ru", "settings_detail_short", "Кратко, подробности ждут в буферах");
            Add("ru", "settings_detail_full", "Полные описания");
            Add("ru", "settings_key_repeat", "Скорость повтора клавиш");
            Add("ru", "settings_key_repeat_slow", "Медленно");
            Add("ru", "settings_key_repeat_normal", "Обычно");
            Add("ru", "settings_key_repeat_fast", "Быстро");
            Add("ru", "settings_debug", "Журнал отладки");
            Add("ru", "settings_on", "Включено");
            Add("ru", "settings_off", "Выключено");
            Add("ru", "settings_hint", "Нажмите F2 для настроек доступности.");

            // Polish
            Add("pl", "settings_opened", "Ustawienia dostepnosci. Strzalki gora i dol zmieniaja ustawienie, lewo i prawo zmieniaja wartosc, Escape zamyka.");
            Add("pl", "settings_closed", "Ustawienia zamkniete.");
            Add("pl", "settings_help", "Ustawienia dostepnosci. Gora i dol: zmiana ustawienia. Lewo i prawo: zmiana wartosci. Enter: nastepna wartosc. Escape lub F2: zamknij.");
            Add("pl", "settings_first", "Pierwsze ustawienie.");
            Add("pl", "settings_last", "Ostatnie ustawienie.");
            Add("pl", "settings_language", "Jezyk");
            Add("pl", "settings_language_auto", "Automatycznie");
            Add("pl", "settings_language_partial", "tlumaczenie czesciowe, reszta czytana po angielsku");
            Add("pl", "settings_detail", "Poziom szczegolow");
            Add("pl", "settings_detail_short", "Krotko, szczegoly czekaja w buforach");
            Add("pl", "settings_detail_full", "Pelne opisy");
            Add("pl", "settings_key_repeat", "Szybkosc powtarzania klawiszy");
            Add("pl", "settings_key_repeat_slow", "Wolno");
            Add("pl", "settings_key_repeat_normal", "Normalnie");
            Add("pl", "settings_key_repeat_fast", "Szybko");
            Add("pl", "settings_debug", "Dziennik debugowania");
            Add("pl", "settings_on", "Wlaczone");
            Add("pl", "settings_off", "Wylaczone");
            Add("pl", "settings_hint", "Nacisnij F2, aby otworzyc ustawienia dostepnosci.");

            // Turkish
            Add("tr", "settings_opened", "Erisilebilirlik ayarlari. Yukari ve asagi ok tuslari ayarlar arasinda gezinir, sol ve sag ayari degistirir, Escape kapatir.");
            Add("tr", "settings_closed", "Ayarlar kapatildi.");
            Add("tr", "settings_help", "Erisilebilirlik ayarlari. Yukari ve asagi: ayar degistir. Sol ve sag: degeri degistir. Enter: sonraki deger. Escape veya F2: kapat.");
            Add("tr", "settings_first", "Ilk ayar.");
            Add("tr", "settings_last", "Son ayar.");
            Add("tr", "settings_language", "Dil");
            Add("tr", "settings_language_auto", "Otomatik");
            Add("tr", "settings_language_partial", "kismi ceviri, gerisi Ingilizce seslendirilir");
            Add("tr", "settings_detail", "Ayrinti duzeyi");
            Add("tr", "settings_detail_short", "Kisa, ayrintilar arabelleklerde bekler");
            Add("tr", "settings_detail_full", "Tam aciklamalar");
            Add("tr", "settings_key_repeat", "Tus tekrar hizi");
            Add("tr", "settings_key_repeat_slow", "Yavas");
            Add("tr", "settings_key_repeat_normal", "Normal");
            Add("tr", "settings_key_repeat_fast", "Hizli");
            Add("tr", "settings_debug", "Hata ayiklama kaydi");
            Add("tr", "settings_on", "Acik");
            Add("tr", "settings_off", "Kapali");
            Add("tr", "settings_hint", "Erisilebilirlik ayarlari icin F2'ye basin.");

            // Japanese
            Add("ja", "settings_opened", "アクセシビリティ設定。上下の矢印キーで設定を切り替え、左右で値を変更、Escapeで閉じます。");
            Add("ja", "settings_closed", "設定を閉じました。");
            Add("ja", "settings_help", "アクセシビリティ設定。上下: 設定の切り替え。左右: 値の変更。Enter: 次の値。EscapeまたはF2: 閉じる。");
            Add("ja", "settings_first", "最初の設定です。");
            Add("ja", "settings_last", "最後の設定です。");
            Add("ja", "settings_language", "言語");
            Add("ja", "settings_language_auto", "自動");
            Add("ja", "settings_language_partial", "部分的な翻訳です。残りは英語で読み上げられます");
            Add("ja", "settings_detail", "詳細レベル");
            Add("ja", "settings_detail_short", "簡潔。詳細はバッファで確認できます");
            Add("ja", "settings_detail_full", "詳しい説明");
            Add("ja", "settings_key_repeat", "キーリピート速度");
            Add("ja", "settings_key_repeat_slow", "遅い");
            Add("ja", "settings_key_repeat_normal", "標準");
            Add("ja", "settings_key_repeat_fast", "速い");
            Add("ja", "settings_debug", "デバッグログ");
            Add("ja", "settings_on", "オン");
            Add("ja", "settings_off", "オフ");
            Add("ja", "settings_hint", "F2でアクセシビリティ設定を開きます。");

            // Korean
            Add("ko", "settings_opened", "접근성 설정. 위아래 화살표로 설정을 이동하고, 좌우로 값을 바꾸며, Escape로 닫습니다.");
            Add("ko", "settings_closed", "설정을 닫았습니다.");
            Add("ko", "settings_help", "접근성 설정. 위아래: 설정 이동. 좌우: 값 변경. Enter: 다음 값. Escape 또는 F2: 닫기.");
            Add("ko", "settings_first", "첫 번째 설정입니다.");
            Add("ko", "settings_last", "마지막 설정입니다.");
            Add("ko", "settings_language", "언어");
            Add("ko", "settings_language_auto", "자동");
            Add("ko", "settings_language_partial", "부분 번역이며 나머지는 영어로 읽습니다");
            Add("ko", "settings_detail", "상세 수준");
            Add("ko", "settings_detail_short", "간략, 자세한 내용은 버퍼에 있습니다");
            Add("ko", "settings_detail_full", "전체 설명");
            Add("ko", "settings_key_repeat", "키 반복 속도");
            Add("ko", "settings_key_repeat_slow", "느리게");
            Add("ko", "settings_key_repeat_normal", "보통");
            Add("ko", "settings_key_repeat_fast", "빠르게");
            Add("ko", "settings_debug", "디버그 로그");
            Add("ko", "settings_on", "켜짐");
            Add("ko", "settings_off", "꺼짐");
            Add("ko", "settings_hint", "F2를 누르면 접근성 설정이 열립니다.");

            // Simplified Chinese
            Add("zh-Hans", "settings_opened", "无障碍设置。上下方向键切换设置，左右键更改数值，Escape关闭。");
            Add("zh-Hans", "settings_closed", "设置已关闭。");
            Add("zh-Hans", "settings_help", "无障碍设置。上下：切换设置。左右：更改数值。Enter：下一个数值。Escape或F2：关闭。");
            Add("zh-Hans", "settings_first", "第一项设置。");
            Add("zh-Hans", "settings_last", "最后一项设置。");
            Add("zh-Hans", "settings_language", "语言");
            Add("zh-Hans", "settings_language_auto", "自动");
            Add("zh-Hans", "settings_language_partial", "部分翻译，其余以英语朗读");
            Add("zh-Hans", "settings_detail", "详细程度");
            Add("zh-Hans", "settings_detail_short", "简短，详细内容保存在缓冲区");
            Add("zh-Hans", "settings_detail_full", "完整描述");
            Add("zh-Hans", "settings_key_repeat", "按键重复速度");
            Add("zh-Hans", "settings_key_repeat_slow", "慢");
            Add("zh-Hans", "settings_key_repeat_normal", "正常");
            Add("zh-Hans", "settings_key_repeat_fast", "快");
            Add("zh-Hans", "settings_debug", "调试日志");
            Add("zh-Hans", "settings_on", "开");
            Add("zh-Hans", "settings_off", "关");
            Add("zh-Hans", "settings_hint", "按F2打开无障碍设置。");

            // Traditional Chinese
            Add("zh-Hant", "settings_opened", "無障礙設定。上下方向鍵切換設定，左右鍵變更數值，Escape關閉。");
            Add("zh-Hant", "settings_closed", "設定已關閉。");
            Add("zh-Hant", "settings_help", "無障礙設定。上下：切換設定。左右：變更數值。Enter：下一個數值。Escape或F2：關閉。");
            Add("zh-Hant", "settings_first", "第一項設定。");
            Add("zh-Hant", "settings_last", "最後一項設定。");
            Add("zh-Hant", "settings_language", "語言");
            Add("zh-Hant", "settings_language_auto", "自動");
            Add("zh-Hant", "settings_language_partial", "部分翻譯，其餘以英語朗讀");
            Add("zh-Hant", "settings_detail", "詳細程度");
            Add("zh-Hant", "settings_detail_short", "簡短，詳細內容保存在緩衝區");
            Add("zh-Hant", "settings_detail_full", "完整描述");
            Add("zh-Hant", "settings_key_repeat", "按鍵重複速度");
            Add("zh-Hant", "settings_key_repeat_slow", "慢");
            Add("zh-Hant", "settings_key_repeat_normal", "正常");
            Add("zh-Hant", "settings_key_repeat_fast", "快");
            Add("zh-Hant", "settings_debug", "偵錯記錄");
            Add("zh-Hant", "settings_on", "開");
            Add("zh-Hant", "settings_off", "關");
            Add("zh-Hant", "settings_hint", "按F2開啟無障礙設定。");
        }
    }
}
