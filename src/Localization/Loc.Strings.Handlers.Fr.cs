namespace WildfrostAccessibility
{
    /// <summary>
    /// The French part of the screen handler string table: Town, ContinueRun,
    /// Map, Battle, CharacterSelect and its tribes, MainMenu, BattleWin,
    /// CampaignEnd, the Daily Voyage balloon, the deckpack inventory, the map
    /// node categories, the pause menu, the story events, and the shared item
    /// descriptions.
    /// </summary>
    public static partial class Loc
    {
        private static void RegisterHandlerStringsFrench()
        {
            // ----- French -------------------------------------------------------

            Add("fr", "stat_health", "{0} points de vie");
            Add("fr", "stat_health_of_max", "{0} sur {1} points de vie");
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
            Add("fr", "shop_price", "Coute {0} d'or.");
            Add("fr", "shop_price_afford", "Coute {0} d'or. Tu as {1}.");
            Add("fr", "shop_price_cant_afford", "Coute {0} d'or. Tu ne peux pas te le permettre; tu as {1}.");
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
            Add("fr", "charselect_tribes", "Choisissez une tribu. Utilisez les fleches haut et bas pour passer d'une tribu a l'autre; le nom et le style de chaque tribu sont lus. La fleche droite lit les chefs et le deck de depart de la tribu, ou maintenez Controle et appuyez sur haut pour les parcourir. Entree choisit la tribu selectionnee, ou Echap pour revenir en arriere.");
            Add("fr", "charselect_leaders", "Choisissez votre chef. Les fleches changent de chef, Entree selectionne.");
            Add("fr", "charselect_chosen", "{0} choisi. Entree confirme, Echap remet la carte.");
            Add("fr", "charselect_chosen_generic", "Carte choisie. Entree confirme, Echap la remet.");
            Add("fr", "charselect_cancelled", "Choix annule, retour a la liste.");
            Add("fr", "charselect_pets", "Choisissez votre familier de depart. Les fleches changent, Entree selectionne.");
            Add("fr", "charselect_starting", "C'est parti! Le voyage commence.");
            Add("fr", "tribe_locked", "{0}, verrouillee");
            Add("fr", "tribe_locked_hint", "Les nouvelles tribus se debloquent a mesure que la jauge de progression du village se remplit: continuez a gagner des combats et a terminer des parties.");
            Add("fr", "tribe_locked_building", "Le deblocage apparait au {0}.");
            Add("fr", "tribe_locked_blocked", "{0} est verrouillee et ne peut pas etre choisie. {1}");
            Add("fr", "charselect_tribes_locked", "Les tribus que vous n'avez pas encore debloquees restent affichees: elles sont annoncees comme verrouillees et ne peuvent pas etre choisies.");
            Add("fr", "inspect_no_confirm", "Ce panneau ne peut pas etre confirme ici. Echap remet la carte.");
            Add("fr", "help_charselect", "Selection du personnage, en trois etapes. D'abord une tribu: les fleches haut et bas passent d'une tribu a l'autre et lisent le nom et le style de chacune, la fleche droite lit les chefs et le deck de depart de la tribu, et Controle plus haut les parcourt dans le tampon de lecture. Entree choisit la tribu selectionnee. Ensuite un chef et un familier de depart, parcourus avec les fleches, Entree pour choisir. Apres avoir choisi une carte, Entree confirme et continue, Echap la remet. I inspecte la carte selectionnee.");
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
            Add("fr", "map_fork_here", "Le chemin se separe ici et les branches ne se rejoignent jamais: en choisir une abandonne les autres.");
            Add("fr", "map_fork_rejoins", "les routes se rejoignent, rien n'est perdu");
            Add("fr", "map_fork_gives_up", "ce choix abandonne: {0}");
            Add("fr", "map_fork_gives_up_more", "ce choix abandonne: {0}, et {1} de plus");
            Add("fr", "map_fork_gives_up_unseen", "ce choix abandonne {0} lieux plus loin sur l'autre branche");
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
            Add("fr", "charm_gained", "Talisman obtenu! {0}");
            Add("fr", "charm_gained_hint", "Entree le fixe maintenant sur une carte, Echap le garde dans l'inventaire pour plus tard.");
            Add("fr", "charm_reserved", "{0} garde dans l'inventaire. Fixez-le a tout moment avec P.");
            Add("fr", "charm_assign_button", "{0} Entree fixe ce talisman maintenant sur une carte, Echap le garde dans l'inventaire pour plus tard.");
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
            // Map node categories
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
            // Battle: turn flow, the four groups, playing and moving cards,
            // combat events, in-fight card readouts and the navigation edge cue
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
            Add("fr", "battle_use_on_hand", "Jouer sans cible");
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
            Add("fr", "battle_your_unit", "votre {0}");
            Add("fr", "battle_enemy_unit", "{0} ennemi");
            Add("fr", "help_battle", "Bataille. Haut et bas changent de groupe: main, votre plateau, plateau ennemi, cloche et piles. Gauche et droite se deplacent dans le groupe. Entree sur une carte de la main la prend; en la tenant, haut et bas changent de rangee, gauche et droite se deplacent dans la rangee, rien ne revient au debut; Entree la pose. Entree sur une de vos unites du plateau la prend pour la deplacer: une case libre la deplace, une case occupee echange ou pousse, la zone de rappel la retire du plateau. Deplacer et rappeler sont des actions gratuites qui ne terminent pas votre tour. Echap repose une carte prise. I inspecte la carte selectionnee, Echap ferme l'inspection. Jouer une carte ou sonner la cloche termine votre tour. Touches de lecture: H main, B plateau, W vagues, R cloche, T tour, G or, M cloches de modificateur, Ctrl C compteurs allies, Ctrl Maj C compteurs ennemis, Ctrl H sante des allies, Ctrl Maj H sante des ennemis. P ouvre votre inventaire. Appuyez sur O pour le menu du jeu avec les reglages.");
            Add("fr", "battle_unit_picked_up", "{0} prise du plateau.");
            Add("fr", "battle_move_hint", "Les fleches choisissent une case de destination ou la zone de rappel, Entree confirme, Echap annule.");
            Add("fr", "tutorial_drag_hint", "Pour selectionner et placer: appuyez sur Entree sur la carte, choisissez la destination avec les fleches, puis appuyez encore sur Entree.");
            Add("fr", "tutorial_inspect_hint", "Vous pouvez aussi maintenir Ctrl et appuyer sur haut pour parcourir tous les details de la carte selectionnee, un par un. Ctrl plus gauche ou droite passe aux autres tampons de lecture.");
            Add("fr", "tutorial_counter_keys_hint", "Inutile de suivre les compteurs carte par carte: appuyez sur Ctrl C pour entendre chacune de vos unites et dans combien de tours elle agit, et Ctrl Maj C pour la meme chose du cote ennemi.");
            Add("fr", "tutorial_health_keys_hint", "Pour savoir qui rappeler, appuyez sur Ctrl H pour la sante de vos compagnons, ou Ctrl Maj H pour celle des ennemis.");
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
            Add("fr", "battle_kill_combo", "Combo x{0} : {0} eliminations ce tour.");
            Add("fr", "battle_kill_combo_gold", "Combo x{0} : {0} eliminations ce tour, {1} or bonus.");
            Add("fr", "battle_gold_dropped", "{0} d'or.");
            Add("fr", "battle_crown_deploy_one", "Une carte couronnee dans votre main se deploie avant la bataille: appuyez sur Entree dessus pour la placer maintenant.");
            Add("fr", "battle_crown_deploy", "{0} cartes couronnees dans votre main se deploient avant la bataille: placez-les maintenant.");
            Add("fr", "card_crowned", "Couronnee, se deploie au debut de la bataille.");
            Add("fr", "card_charm_one", "Talisman: {0}.");
            Add("fr", "card_charms", "{0} talismans: {1}.");
            Add("fr", "card_token_one", "Jeton: {0}.");
            Add("fr", "card_tokens", "{0} jetons: {1}.");
            Add("fr", "counter_frozen", "compteur gele par la neige");
            Add("fr", "battle_status_damage", "{0} subit {1} degats de {2}.");
            Add("fr", "battle_shell_blocked", "{0} : Shell bloque {1} degats.");
            Add("fr", "battle_shell_blocked_all", "{2} frappe {0}. Shell bloque les {1} degats.");
            Add("fr", "battle_counter_reduced", "{0} : compteur reduit de {1}, agit dans {2}.");
            Add("fr", "battle_attack_gain", "{0} gagne {1} d'attaque, maintenant {2}.");
            Add("fr", "battle_attack_lose", "{0} perd {1} d'attaque, maintenant {2}.");
            Add("fr", "battle_counter_gain", "{0} : compteur augmente de {1}, agit dans {2}.");
            Add("fr", "battle_counters_allies", "Compteurs allies");
            Add("fr", "battle_counters_enemies", "Compteurs ennemis");
            Add("fr", "battle_counters_none_ally", "Aucun allie avec compteur.");
            Add("fr", "battle_counters_none_enemy", "Aucun ennemi avec compteur.");
            Add("fr", "battle_health_allies", "Sante des allies");
            Add("fr", "battle_health_enemies", "Sante des ennemis");
            Add("fr", "battle_health_none_ally", "Aucun allie sur le plateau.");
            Add("fr", "battle_health_none_enemy", "Aucun ennemi sur le plateau.");
            Add("fr", "nav_edge", "Bord.");
            Add("fr", "battle_not_a_target", "Cible invalide pour cette carte.");
            // Taking an item from an event, then the mechanic wording the card
            // description builder assembles: triggers, targets and summons
            Add("fr", "event_item_taken", "{0} pris.");
            Add("fr", "status_mech_applies", "applique {0} {1}");
            Add("fr", "status_mech_damage", "inflige {0} degats");
            Add("fr", "mech_when_card_destroyed", "quand une autre carte est detruite");
            Add("fr", "mech_when_ally_killed", "quand un allie est tue");
            Add("fr", "mech_when_unit_killed", "quand une unite est tuee");
            Add("fr", "mech_when_clunker_destroyed", "quand un Clunker est detruit");
            Add("fr", "mech_when_hit", "quand il est touche");
            Add("fr", "mech_when_unit_hit", "quand une autre unite est touchee");
            Add("fr", "mech_when_damage_taken", "quand il subit des degats");
            Add("fr", "mech_when_healed", "quand il est soigne");
            Add("fr", "mech_when_ally_healed", "quand un allie est soigne");
            Add("fr", "mech_when_deployed", "au deploiement");
            Add("fr", "mech_when_destroyed", "quand il est detruit");
            Add("fr", "mech_when_drawn", "quand il est pioche");
            Add("fr", "mech_on_kill", "apres une elimination");
            Add("fr", "mech_on_turn", "a son tour");
            Add("fr", "mech_every_turn", "chaque tour");
            Add("fr", "mech_after_attack", "apres avoir attaque");
            Add("fr", "mech_on_hit", "quand il touche");
            Add("fr", "mech_to_self", "a lui-meme");
            Add("fr", "mech_to_allies", "a tous les allies");
            Add("fr", "mech_to_allies_row", "aux allies de sa rangee");
            Add("fr", "mech_to_front_ally", "a l'allie de devant");
            Add("fr", "mech_to_enemies", "a tous les ennemis");
            Add("fr", "mech_to_front_enemy", "a l'ennemi de devant");
            Add("fr", "mech_to_attacker", "a l'attaquant");
            Add("fr", "mech_to_target", "a sa cible");
            Add("fr", "mech_to_random_ally", "a un allie au hasard");
            Add("fr", "mech_to_random_enemy", "a un ennemi au hasard");
            Add("fr", "mech_to_hand", "aux cartes en main");
            Add("fr", "mech_summons", "Invoque {0}: {1}.");
            Add("fr", "mech_summons_bare", "Invoque {0}.");
            Add("fr", "mech_summon_counter", "agit tous les {0}");
            Add("fr", "mech_trigger_meaning", "Se declencher signifie que cette carte attaque des que cela arrive, sur sa propre cible, en plus de son tour normal. Son compte a rebours n'est pas remis a zero, l'attaque est donc gratuite.");
            Add("fr", "mech_trigger_retaliate", "Quand quelque chose frappe cette carte, elle riposte aussitot contre l'attaquant, en plus de son tour normal.");
            // Card badges: injuries, mentioned keywords and upgrade kinds
            Add("fr", "card_injured_one", "Blesse.");
            Add("fr", "card_injured", "Blesse x{0}.");
            Add("fr", "card_mentions", "Mentionne {0}");
            Add("fr", "upgrade_charm", "talisman");
            Add("fr", "upgrade_crown", "couronne");
            Add("fr", "upgrade_token", "jeton");
            // Back on the battle screen: Last Stand, injuries, the crown
            // holder, and the redraw, wave and modifier bells
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
            // Game menu (the pause screen): tabs, settings rows and lore pages
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
            // Story events (Event scene, cinema bar text), the inspect view
            // and the shared prompt panel
            Add("fr", "event_prompt_action", "Appuyez sur Entree.");
            Add("fr", "event_crack", "Fissure {0} sur 4.");
            Add("fr", "select_blocked", "Impossible de choisir ceci pour le moment.");
            Add("fr", "select_blocked_reason", "Pas encore autorise. {0}");
            Add("fr", "inspect_opened", "Inspection de {0}. Echap pour fermer.");
            Add("fr", "inspect_closed", "Inspection fermee.");
            Add("fr", "nothing_to_inspect", "Rien a inspecter ici.");
            Add("fr", "help_panel_hint", "Les fleches gauche et droite choisissent un bouton, Entree l'active, Echap revient en arriere.");
            Add("fr", "help_panel_no_back", "Cette invite demande une reponse. Utilisez gauche et droite, puis Entree.");
            // Translated later and kept in key order: the town overlays and
            // unlock buildings, the shrine challenges, the character select
            // back and position labels, the Icebreaker Hut's map events, the
            // tribe names, blurbs and hall, and the in-front placement
            // tutorial hint. Tribe names themselves stay as they are —
            // Snowdwellers, Shademancers and Clunkmasters are proper nouns
            // the game never translates either.
            Add("fr", "building_back", "Retour.");
            Add("fr", "building_closed", "Ferme.");
            Add("fr", "building_overlay_hint", "Les fleches naviguent entre les bannieres, Entree en ouvre une, Echap quitte, I relit.");
            Add("fr", "challenge_hidden", "{0}, defi cache");
            Add("fr", "challenge_stone", "Defi");
            Add("fr", "charselect_back", "Retour");
            Add("fr", "charselect_leader_pos", "Chef {0} sur {1}: {2}");
            Add("fr", "charselect_pet_pos", "Familier {0} sur {1}: {2}");
            Add("fr", "event_node_CampaignNodeCharmShop", "Boutique de charmes");
            Add("fr", "event_node_CampaignNodeCopyItem", "Copier un objet");
            Add("fr", "event_node_CampaignNodeCurseItems", "Objets maudits");
            Add("fr", "icebreaker_hint", "Les fleches naviguent entre les evenements de carte, Entree lit ce que fait l'un d'eux, Echap quitte, I relit le resume.");
            Add("fr", "icebreaker_unlock_intro", "Terminez ce defi pour debloquer le prochain evenement de carte:");
            Add("fr", "icebreaker_unlocked", "{0} evenements de carte debloques sur {1}: {2}.");
            Add("fr", "icebreaker_unlocked_none", "Aucun de ses {0} evenements de carte n'est encore debloque.");
            Add("fr", "overlay_browse", "{0} elements. Utilisez les fleches pour les parcourir.");
            Add("fr", "overlay_item", "{0}, {1} sur {2}");
            Add("fr", "overlay_position", "{0} sur {1}");
            Add("fr", "shrine_hint", "Haut et bas basculent entre les defis en cours et termines, gauche et droite les parcourent.");
            Add("fr", "shrine_none_completed", "Aucun defi termine.");
            Add("fr", "shrine_none_incomplete", "Aucun defi en cours.");
            Add("fr", "shrine_row_completed", "Defis termines");
            Add("fr", "shrine_row_incomplete", "Defis en cours");
            Add("fr", "shrine_summary", "{0} en cours, {1} termines.");
            Add("fr", "tribe_banner", "Banniere de tribu");
            Add("fr", "tribe_desc_Basic", "La tribu de depart et la plus accessible aux debutants. Elle gele les ennemis avec Givre et Neige pour leur faire sauter des tours, et augmente son attaque avec Bonus.");
            Add("fr", "tribe_desc_Clunk", "Des bricoleurs qui combattent avec des Ferrailleurs, de lourdes unites de rebut, et de la Ferraille. Ils submergent l'ennemi par la masse et la machinerie.");
            Add("fr", "tribe_desc_Magic", "Une tribu agressive et risquee batie sur l'Ombre et l'Epice. Elle accumule de l'attaque temporaire pour frapper vite et fort, mais ses unites sont fragiles.");
            Add("fr", "tribe_leaders", "Chefs: {0}.");
            Add("fr", "tribe_leaders_random", "Les chefs sont generes au hasard: vous en choisirez un parmi trois apres avoir choisi cette tribu.");
            Add("fr", "tribe_no_roster", "Aucun chef ni deck de depart n'est indique pour cette tribu.");
            Add("fr", "tribe_starting_deck", "Deck de depart: {0}.");
            Add("fr", "tribe_unlock_intro", "Terminez ce defi pour debloquer la prochaine tribu:");
            Add("fr", "tribehall_hint", "Les fleches naviguent entre les bannieres de tribu, Entree ouvre la page de la tribu selectionnee, Echap quitte, I relit le resume.");
            Add("fr", "tribehall_unlocked", "{0} tribus debloquees sur {1}: {2}.");
            Add("fr", "tribehall_unlocked_none", "Aucune de ses {0} tribus n'est encore debloquee.");
            Add("fr", "tutorial_drag_hint_infront", "Pour faire cela: appuyez sur Entree sur la carte, choisissez avec les fleches l'unite devant laquelle la placer, puis appuyez sur Entree — votre carte prend cette place et repousse l'unite en arriere.");
            Add("fr", "unlock_all_done", "Tout est debloque ici.");
            Add("fr", "unlock_detail_back", "Echap revient en arriere.");
            Add("fr", "unlock_entry_locked", "{0} est encore verrouille.");
            Add("fr", "unlock_next_intro", "Prochain deblocage:");
            Add("fr", "unlock_slot_locked", "Emplacement verrouille");
            Add("fr", "unlock_state_locked", "verrouille");
            Add("fr", "unlock_state_unlocked", "debloque");
            Add("fr", "unlockhut_hint", "Les fleches naviguent entre les emplacements, Echap quitte, I relit le resume.");
            Add("fr", "unlockhut_unlocked", "{0} debloques sur {1}: {2}.");
            Add("fr", "unlockhut_unlocked_none", "Aucun de ses {0} emplacements n'est encore ouvert.");

            // Closing the prompt panel, and the event screen's F1 help
            Add("fr", "help_panel_closed", "Ferme.");
            Add("fr", "help_event", "Ecran d'evenement. Un evenement d'histoire sur votre voyage; son titre et son texte sont lus quand ils apparaissent. Les fleches naviguent entre les elements, Entree active. Dans une boutique, chaque article annonce son prix et si vous pouvez vous le permettre; appuyez sur Entree dessus pour l'acheter, et sur G pour entendre votre or actuel. I inspecte la carte selectionnee, comme le clic droit pour les joueurs voyants, Echap ferme l'inspection. P ouvre votre inventaire.");
        }
    }
}
