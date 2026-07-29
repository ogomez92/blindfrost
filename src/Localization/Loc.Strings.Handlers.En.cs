using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace WildfrostAccessibility
{
    /// <summary>
    /// The English half of the screen handler string table (Town, ContinueRun,
    /// Map, Battle and the shared item descriptions).
    /// </summary>
    public static partial class Loc
    {
        private static void RegisterHandlerStringsEnglish()
        {
            // ----- English -----------------------------------------------------

            // Shared stats and items
            Add("en", "stat_health", "{0} health");
            Add("en", "stat_health_of_max", "{0} of {1} health");
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
            Add("en", "shop_price", "Costs {0} gold.");
            Add("en", "shop_price_afford", "Costs {0} gold. You have {1}.");
            Add("en", "shop_price_cant_afford", "Costs {0} gold. You can't afford it; you have {1}.");

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
            Add("en", "charselect_leader_pos", "Leader {0} of {1}: {2}");
            Add("en", "charselect_pet_pos", "Pet {0} of {1}: {2}");
            Add("en", "charselect_chosen", "{0} chosen. Press Enter to confirm, or Escape to put the card back.");
            Add("en", "charselect_chosen_generic", "Card chosen. Press Enter to confirm, or Escape to put it back.");
            Add("en", "charselect_cancelled", "Choice cancelled, back to browsing.");
            Add("en", "charselect_pets", "Choose your starting pet. Arrow keys browse, Enter selects.");
            Add("en", "charselect_starting", "Let's go! Starting the journey.");
            Add("en", "inspect_no_confirm", "This panel cannot be confirmed here. Escape puts the card back.");
            Add("en", "help_charselect", "Character selection, in three stages. First a tribe: up and down arrows move between the tribes and read each tribe's name and playstyle, the right arrow reads that tribe's leaders and starting deck, and Control plus up steps through them in the review buffer. Enter chooses the focused tribe. Then a leader and a starting pet, browsed with the arrow keys, Enter to pick. After choosing a card, Enter confirms and continues, Escape puts it back. I inspects the focused card.");

            // Tribe names and playstyle blurbs, keyed by the ClassData internal id.
            // The game has no readable tribe name or description, so the mod supplies
            // them; unmapped/modded tribes fall back to their cleaned asset name.
            Add("en", "tribe_name_Basic", "Snowdwellers");
            Add("en", "tribe_desc_Basic", "The starting tribe and the most beginner friendly. They freeze enemies with Frost and Snow so they skip turns, and grow their attack with Bonus.");
            Add("en", "tribe_name_Magic", "Shademancers");
            Add("en", "tribe_desc_Magic", "An aggressive, high risk tribe built around Shade and Spice. They stack up temporary attack to hit fast and hard, but their units are fragile.");
            Add("en", "tribe_name_Clunk", "Clunkmasters");
            Add("en", "tribe_desc_Clunk", "Tinkerers who fight with Clunkers, heavy scrap units, and Junk. They overwhelm the enemy with sheer mass and machinery.");
            Add("en", "tribe_leaders", "Leaders: {0}.");
            Add("en", "tribe_leaders_random", "Leaders are randomly generated: you choose from three after picking this tribe.");
            Add("en", "tribe_starting_deck", "Starting deck: {0}.");
            Add("en", "tribe_no_roster", "No leaders or starting deck listed for this tribe.");
            Add("en", "charselect_back", "Go back");

            // Tribes this save has not earned. The game still puts their flags on
            // the select screen (its own filter compares the wrong ScriptableObject
            // instances), so the mod marks them and refuses them on Enter.
            Add("en", "tribe_locked", "{0}, locked");
            Add("en", "tribe_locked_hint", "New tribes unlock as the town progress meter fills, so keep winning battles and finishing runs.");
            Add("en", "tribe_locked_building", "The unlock shows up at the {0}.");
            Add("en", "tribe_locked_blocked", "{0} is locked, so it cannot be chosen. {1}");
            Add("en", "charselect_tribes_locked", "Tribes you have not unlocked yet are still shown: they are read out as locked and cannot be chosen.");

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
            // The town's unlock buildings: Tribe Hall, Pet House, Inventor's Hut,
            // companion hut, Icebreaker Hut. All browsed the same way.
            Add("en", "unlock_state_unlocked", "unlocked");
            Add("en", "unlock_state_locked", "locked");
            Add("en", "unlock_slot_locked", "Locked slot");
            Add("en", "unlock_entry_locked", "{0} is still locked.");
            Add("en", "unlock_next_intro", "Next unlock:");
            Add("en", "unlock_all_done", "Everything here is unlocked.");
            Add("en", "unlock_detail_back", "Escape goes back.");
            Add("en", "unlockhut_unlocked", "{0} of {1} unlocked: {2}.");
            Add("en", "unlockhut_unlocked_none", "None of its {0} slots are open yet.");
            Add("en", "unlockhut_hint", "Arrow keys move between the slots, Escape leaves, I reads the summary again.");
            Add("en", "tribehall_unlocked", "{0} of {1} tribes unlocked: {2}.");
            Add("en", "tribehall_unlocked_none", "None of its {0} tribes are unlocked yet.");
            Add("en", "tribehall_hint", "Arrow keys move between the tribe banners, Enter opens the focused tribe's page, Escape leaves, I reads the summary again.");
            Add("en", "icebreaker_unlocked", "{0} of {1} map events unlocked: {2}.");
            Add("en", "icebreaker_unlocked_none", "None of its {0} map events are unlocked yet.");
            Add("en", "icebreaker_unlock_intro", "Complete this challenge to unlock the next map event:");
            Add("en", "icebreaker_hint", "Arrow keys move between the map events, Enter reads what one does, Escape leaves, I reads the summary again.");
            // Map events the Icebreaker Hut unlocks, keyed by asset name
            Add("en", "event_node_CampaignNodeCopyItem", "Copy an item");
            Add("en", "event_node_CampaignNodeCharmShop", "Charm shop");
            Add("en", "event_node_CampaignNodeCurseItems", "Cursed items");
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
            Add("en", "map_fork_here", "The path splits here and the branches never meet again: choosing one gives the others up.");
            Add("en", "map_fork_rejoins", "the routes rejoin, so nothing is given up");
            Add("en", "map_fork_gives_up", "taking this gives up: {0}");
            Add("en", "map_fork_gives_up_more", "taking this gives up: {0}, and {1} more");
            Add("en", "map_fork_gives_up_unseen", "taking this gives up {0} locations further along the other branch");
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
            Add("en", "charm_gained", "Charm gained! {0}");
            Add("en", "charm_gained_hint", "Press Enter to equip it on a card now, or Escape to keep it in your inventory for later.");
            Add("en", "charm_reserved", "{0} kept in your inventory. Equip it any time by pressing P.");
            Add("en", "charm_assign_button", "{0} Enter equips this charm on a card now, Escape keeps it in your inventory for later.");
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
            Add("en", "battle_use_on_hand", "Play without a target");
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
            Add("en", "battle_your_unit", "your {0}");
            Add("en", "battle_enemy_unit", "enemy {0}");
            Add("en", "help_battle", "Battle. Up and down arrows switch groups: hand, your board, enemy board, bell and piles. Left and right arrows move within a group. Enter on a hand card picks it up; while holding it, up and down move between rows, left and right move along the row, nothing wraps around; Enter places it. Enter on one of your units on the board picks it up to move it: a free slot moves it, an occupied slot swaps or shoves, the recall zone takes it off the board. Moving and recalling are free actions that do not end your turn. Escape puts a picked-up card back. I inspects the focused card, Escape closes the inspect view. Playing a card or ringing the bell ends your turn. Readout keys: H hand, B board, W waves, R bell, T turn, G gold, M modifier bells, Control C ally counters, Control E enemy counters, Control H ally health, Control Shift H enemy health. P opens your inventory. Press O for the game menu with settings.");
            Add("en", "battle_unit_picked_up", "{0} picked up from the board.");
            Add("en", "battle_move_hint", "Arrow keys choose a destination slot or the recall zone, Enter confirms, Escape cancels.");
            Add("en", "tutorial_drag_hint", "To select and place: press Enter on the card, choose the destination with the arrow keys, then press Enter again.");
            Add("en", "tutorial_drag_hint_infront", "To do this: press Enter on the card, use the arrow keys to choose the unit you want it in front of, then press Enter — your card takes that spot and pushes the unit back.");
            Add("en", "tutorial_inspect_hint", "You can also hold Control and press the up arrow to step through everything about the focused card, one detail at a time. Control plus left or right switches to other review buffers.");
            Add("en", "tutorial_counter_keys_hint", "You do not have to track the counters one card at a time: press Control C to hear each of your units and how many turns until it acts, and Control E for the same on the enemy side.");
            Add("en", "tutorial_health_keys_hint", "To find out who needs pulling back, press Control H for your companions' health, or Control Shift H for the enemies'.");
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
            Add("en", "battle_kill_combo", "Combo x{0}: {0} kills this turn.");
            Add("en", "battle_kill_combo_gold", "Combo x{0}: {0} kills this turn, {1} bonus gold.");
            Add("en", "battle_gold_dropped", "{0} gold.");
            Add("en", "battle_crown_deploy_one", "One crowned card in your hand deploys before the battle: press Enter on it to place it now.");
            Add("en", "battle_crown_deploy", "{0} crowned cards in your hand deploy before the battle: place them now.");
            Add("en", "card_crowned", "Crowned, deploys at battle start.");
            Add("en", "card_charm_one", "Charm: {0}.");
            Add("en", "card_charms", "{0} charms: {1}.");
            Add("en", "card_token_one", "Token: {0}.");
            Add("en", "card_tokens", "{0} tokens: {1}.");
            Add("en", "counter_frozen", "counter frozen by Snow");
            Add("en", "battle_status_damage", "{0} takes {1} {2} damage.");
            Add("en", "battle_shell_blocked", "{0}: Shell blocks {1} damage.");
            Add("en", "battle_shell_blocked_all", "{2} hits {0}. Shell blocks all {1} damage.");
            Add("en", "battle_counter_reduced", "{0}: counter down {1}, acts in {2}.");
            Add("en", "battle_attack_gain", "{0} gains {1} attack, now {2}.");
            Add("en", "battle_attack_lose", "{0} loses {1} attack, now {2}.");
            Add("en", "battle_counter_gain", "{0}: counter up {1}, acts in {2}.");
            Add("en", "battle_counters_allies", "Ally counters");
            Add("en", "battle_counters_enemies", "Enemy counters");
            Add("en", "battle_counters_none_ally", "No allies with counters.");
            Add("en", "battle_counters_none_enemy", "No enemies with counters.");
            Add("en", "battle_health_allies", "Ally health");
            Add("en", "battle_health_enemies", "Enemy health");
            Add("en", "battle_health_none_ally", "No allies on the board.");
            Add("en", "battle_health_none_enemy", "No enemies on the board.");
            Add("en", "nav_edge", "Edge.");
            Add("en", "battle_not_a_target", "Not a valid target for this card.");
            Add("en", "event_item_taken", "Took {0}.");
            Add("en", "status_mech_applies", "applies {0} {1}");
            Add("en", "status_mech_damage", "deals {0} damage");
            Add("en", "mech_when_card_destroyed", "when any other card is destroyed");
            Add("en", "mech_when_ally_killed", "when an ally is killed");
            Add("en", "mech_when_unit_killed", "when a unit is killed");
            Add("en", "mech_when_clunker_destroyed", "when a Clunker is destroyed");
            Add("en", "mech_when_hit", "when hit");
            Add("en", "mech_when_unit_hit", "when another unit is hit");
            Add("en", "mech_when_damage_taken", "when it takes damage");
            Add("en", "mech_when_healed", "when healed");
            Add("en", "mech_when_ally_healed", "when an ally is healed");
            Add("en", "mech_when_deployed", "when deployed");
            Add("en", "mech_when_destroyed", "when destroyed");
            Add("en", "mech_when_drawn", "when drawn");
            Add("en", "mech_on_kill", "on kill");
            Add("en", "mech_on_turn", "on its turn");
            Add("en", "mech_every_turn", "every turn");
            Add("en", "mech_after_attack", "after attacking");
            Add("en", "mech_on_hit", "when it hits");
            Add("en", "mech_to_self", "to itself");
            Add("en", "mech_to_allies", "to all allies");
            Add("en", "mech_to_allies_row", "to allies in its row");
            Add("en", "mech_to_front_ally", "to the front ally");
            Add("en", "mech_to_enemies", "to all enemies");
            Add("en", "mech_to_front_enemy", "to the front enemy");
            Add("en", "mech_to_attacker", "to the attacker");
            Add("en", "mech_to_target", "to its target");
            Add("en", "mech_to_random_ally", "to a random ally");
            Add("en", "mech_to_random_enemy", "to a random enemy");
            Add("en", "mech_to_hand", "to cards in hand");
            Add("en", "mech_summons", "Summons {0}: {1}.");
            Add("en", "mech_summons_bare", "Summons {0}.");
            Add("en", "mech_summon_counter", "acts every {0}");
            Add("en", "mech_trigger_meaning", "Trigger means this card attacks the moment that happens, at its own target, on top of its normal turn. Its countdown is not reset, so the attack is free.");
            Add("en", "mech_trigger_retaliate", "When something hits this card, it strikes back at the attacker straight away, on top of its normal turn.");
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
            Add("en", "help_event", "Event screen. A story event on your journey; its title and story are read as they appear. Arrow keys move between items, Enter activates. In a shop, each ware reads its price and whether you can afford it; press Enter on it to buy, and G to hear your current gold. I inspects the focused card the way right-click does for sighted players, Escape closes the inspect view. P opens your inventory.");

        }
    }
}
