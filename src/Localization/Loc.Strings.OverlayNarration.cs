namespace WildfrostAccessibility
{
    /// <summary>
    /// Strings for the overlay scenes, the visual-moment narration and the
    /// companion limit screen, plus a few handler strings that belong to no
    /// other part: the event card pickup, the CampaignEnd press-again prompt
    /// and the generic screen help. English, German, Spanish and French only;
    /// every other locale falls back to English.
    /// </summary>
    public static partial class Loc
    {
        /// <summary>
        /// Strings for the overlay scenes (boss rewards, card frames, town
        /// unlocks, Frostoscope, journal, credits), the visual-moment
        /// narration (speech bubbles, miniboss arrivals, waves, recoveries,
        /// the final-boss shade), the companion limit screen, and the event
        /// card pickup, CampaignEnd press-again and generic screen help
        /// strings. Registered for English, German, Spanish and French.
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
            Add("en", "narrate_wave_enter", "{0} enters at {1}.");
            Add("en", "narrate_wave_no_room", "{0} more had no room and wait for the next wave.");
            Add("en", "narrate_gold_gained", "Gained {0} gold.");
            Add("en", "recover_one", "{0} has recovered and can fight again.");
            Add("en", "recover_many", "Recovered and able to fight again: {0}.");
            Add("en", "injured_event", "An injured companion from an earlier journey: {0}. Taking them adds them to your party injured, and they must recover before they can fight.");
            Add("en", "injured_event_unnamed", "An injured companion from an earlier journey. Taking them adds them to your party injured, and they must recover before they can fight.");
            Add("en", "narrate_boss_transform", "{0} explodes and transforms! The battle enters a new phase.");
            Add("en", "narrate_shade_flee", "A dark wisp rises from the fallen guardian and flees into the storm.");
            Add("en", "narrate_shade_possess", "A dark wisp rises from the fallen guardian and dives toward your leader!");
            Add("en", "narrate_shade_blocked", "{0} blocks the dark wisp! The Frost is sealed away.");
            Add("en", "narrate_frost_eyes", "{0}'s eyes glow with frost. The Frost has taken hold.");
            Add("en", "narrate_combine", "Your cards swirl together and combine into something new!");

            Add("en", "companion_limit_open", "Too many companions! You have {0} but only {1} can be active. Arrows move between companions, Enter moves the focused one between the active and reserve rows; reserve companions sit out. Press Continue when done.");
            Add("en", "companion_limit_active_list", "Active: {0}.");
            Add("en", "companion_limit_reserve_list", "In reserve: {0}.");
            Add("en", "companion_limit_moved_active", "{0} moved to the active row. {1} of {2} active.");
            Add("en", "companion_limit_moved_reserve", "{0} moved to the reserve row. {1} of {2} active.");
            Add("en", "companion_limit_over", "Over the limit; move a companion to reserve to continue.");
            Add("en", "companion_limit_can_continue", "You can continue now.");
            Add("en", "companion_limit_row_active", "active row");
            Add("en", "companion_limit_row_reserve", "reserve row");

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
            Add("de", "narrate_wave_enter", "{0} erscheint bei {1}.");
            Add("de", "narrate_wave_no_room", "{0} weitere hatten keinen Platz und warten auf die naechste Welle.");
            Add("de", "narrate_gold_gained", "{0} Gold erhalten.");
            Add("de", "recover_one", "{0} ist genesen und kann wieder kaempfen.");
            Add("de", "recover_many", "Genesen und wieder kampfbereit: {0}.");
            Add("de", "injured_event", "Ein verletzter Begleiter aus einer frueheren Reise: {0}. Wenn du ihn mitnimmst, kommt er verletzt in deine Gruppe und muss erst genesen, bevor er kaempfen kann.");
            Add("de", "injured_event_unnamed", "Ein verletzter Begleiter aus einer frueheren Reise. Wenn du ihn mitnimmst, kommt er verletzt in deine Gruppe und muss erst genesen, bevor er kaempfen kann.");
            Add("de", "narrate_boss_transform", "{0} explodiert und verwandelt sich! Der Kampf tritt in eine neue Phase.");
            Add("de", "narrate_shade_flee", "Ein dunkler Schemen steigt aus dem gefallenen Waechter und flieht in den Sturm.");
            Add("de", "narrate_shade_possess", "Ein dunkler Schemen steigt aus dem gefallenen Waechter und stuerzt sich auf deinen Anfuehrer!");
            Add("de", "narrate_shade_blocked", "{0} blockt den dunklen Schemen! Der Frost ist versiegelt.");
            Add("de", "narrate_frost_eyes", "{0}s Augen gluehen frostig. Der Frost hat Besitz ergriffen.");
            Add("de", "narrate_combine", "Deine Karten wirbeln zusammen und verschmelzen zu etwas Neuem!");

            Add("de", "companion_limit_open", "Zu viele Gefaehrten! Du hast {0}, aber nur {1} koennen aktiv sein. Pfeiltasten wechseln zwischen den Gefaehrten, Eingabe verschiebt den fokussierten zwischen aktiver Reihe und Reserve; Reserve-Gefaehrten setzen aus. Druecke Weiter, wenn du fertig bist.");
            Add("de", "companion_limit_active_list", "Aktiv: {0}.");
            Add("de", "companion_limit_reserve_list", "In Reserve: {0}.");
            Add("de", "companion_limit_moved_active", "{0} in die aktive Reihe verschoben. {1} von {2} aktiv.");
            Add("de", "companion_limit_moved_reserve", "{0} in die Reserve verschoben. {1} von {2} aktiv.");
            Add("de", "companion_limit_over", "Ueber dem Limit; verschiebe einen Gefaehrten in die Reserve, um fortzufahren.");
            Add("de", "companion_limit_can_continue", "Du kannst jetzt fortfahren.");
            Add("de", "companion_limit_row_active", "aktive Reihe");
            Add("de", "companion_limit_row_reserve", "Reserve-Reihe");

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
            Add("es", "narrate_wave_enter", "{0} entra en {1}.");
            Add("es", "narrate_wave_no_room", "{0} mas no cabian y esperan a la siguiente oleada.");
            Add("es", "narrate_gold_gained", "Has ganado {0} de oro.");
            Add("es", "recover_one", "{0} se ha recuperado y puede volver a luchar.");
            Add("es", "recover_many", "Recuperados y listos para luchar: {0}.");
            Add("es", "injured_event", "Un companero herido de un viaje anterior: {0}. Si te lo llevas, se une herido a tu grupo y tendra que recuperarse antes de poder luchar.");
            Add("es", "injured_event_unnamed", "Un companero herido de un viaje anterior. Si te lo llevas, se une herido a tu grupo y tendra que recuperarse antes de poder luchar.");
            Add("es", "narrate_boss_transform", "{0} explota y se transforma! La batalla entra en una nueva fase.");
            Add("es", "narrate_shade_flee", "Una voluta oscura surge del guardian caido y huye hacia la tormenta.");
            Add("es", "narrate_shade_possess", "Una voluta oscura surge del guardian caido y se lanza hacia tu lider!");
            Add("es", "narrate_shade_blocked", "{0} bloquea la voluta oscura! La Escarcha queda sellada.");
            Add("es", "narrate_frost_eyes", "Los ojos de {0} brillan con escarcha. La Escarcha ha tomado el control.");
            Add("es", "narrate_combine", "Tus cartas giran juntas y se combinan en algo nuevo!");

            Add("es", "companion_limit_open", "Demasiados companeros! Tienes {0}, pero solo {1} pueden estar activos. Las flechas se mueven entre los companeros, Intro mueve el enfocado entre la fila activa y la reserva; los de reserva no combaten. Pulsa Continuar cuando termines.");
            Add("es", "companion_limit_active_list", "Activos: {0}.");
            Add("es", "companion_limit_reserve_list", "En reserva: {0}.");
            Add("es", "companion_limit_moved_active", "{0} movido a la fila activa. {1} de {2} activos.");
            Add("es", "companion_limit_moved_reserve", "{0} movido a la reserva. {1} de {2} activos.");
            Add("es", "companion_limit_over", "Por encima del limite; mueve un companero a la reserva para continuar.");
            Add("es", "companion_limit_can_continue", "Ya puedes continuar.");
            Add("es", "companion_limit_row_active", "fila activa");
            Add("es", "companion_limit_row_reserve", "fila de reserva");

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
            Add("fr", "narrate_wave_enter", "{0} arrive en {1}.");
            Add("fr", "narrate_wave_no_room", "{0} autres n'avaient pas de place et attendent la prochaine vague.");
            Add("fr", "narrate_gold_gained", "{0} or obtenu.");
            Add("fr", "recover_one", "{0} est retabli et peut se battre de nouveau.");
            Add("fr", "recover_many", "Retablis et prets a se battre: {0}.");
            Add("fr", "injured_event", "Un compagnon blesse d'un voyage precedent: {0}. Si vous l'emmenez, il rejoint votre groupe blesse et devra se retablir avant de pouvoir se battre.");
            Add("fr", "injured_event_unnamed", "Un compagnon blesse d'un voyage precedent. Si vous l'emmenez, il rejoint votre groupe blesse et devra se retablir avant de pouvoir se battre.");
            Add("fr", "narrate_boss_transform", "{0} explose et se transforme! La bataille entre dans une nouvelle phase.");
            Add("fr", "narrate_shade_flee", "Une volute sombre s'eleve du gardien tombe et s'enfuit dans la tempete.");
            Add("fr", "narrate_shade_possess", "Une volute sombre s'eleve du gardien tombe et fonce vers votre chef!");
            Add("fr", "narrate_shade_blocked", "{0} bloque la volute sombre! Le Givre est scelle.");
            Add("fr", "narrate_frost_eyes", "Les yeux de {0} brillent de givre. Le Givre s'est empare de lui.");
            Add("fr", "narrate_combine", "Vos cartes tourbillonnent et fusionnent en quelque chose de nouveau!");

            Add("fr", "companion_limit_open", "Trop de compagnons! Vous en avez {0}, mais seuls {1} peuvent etre actifs. Les fleches naviguent entre les compagnons, Entree deplace celui qui est selectionne entre la rangee active et la reserve; les compagnons en reserve ne combattent pas. Appuyez sur Continuer quand vous avez fini.");
            Add("fr", "companion_limit_active_list", "Actifs: {0}.");
            Add("fr", "companion_limit_reserve_list", "En reserve: {0}.");
            Add("fr", "companion_limit_moved_active", "{0} deplace dans la rangee active. {1} actifs sur {2}.");
            Add("fr", "companion_limit_moved_reserve", "{0} deplace en reserve. {1} actifs sur {2}.");
            Add("fr", "companion_limit_over", "Au-dessus de la limite; mettez un compagnon en reserve pour continuer.");
            Add("fr", "companion_limit_can_continue", "Vous pouvez continuer maintenant.");
            Add("fr", "companion_limit_row_active", "rangee active");
            Add("fr", "companion_limit_row_reserve", "rangee de reserve");
        }

    }
}
