using System.Text.Json;
using MonsterTrain2Poju.Model;

internal static class CardUpgradeMaskChecks
{
    internal static void Run()
    {
        CardUpgradeMaskCard Card(bool state = false, string type = "Monster", int cost = 2, bool xCost = false,
            int size = 2, int target = 1, bool attack = true, bool ability = false, bool graft = false, int visible = 0,
            string[]? subtypes = null, string[]? traits = null, string[]? effects = null, string[]? upgrades = null,
            UpgradeMaskStatus[]? statuses = null, IReadOnlyList<UpgradeMaskStatus>[]? main = null) =>
            new("card", type, "Common", type == "Monster", attack, subtypes ?? ["A", "B"], statuses ?? [], main ?? [],
                traits ?? [], effects ?? [], null, cost, xCost, size, target, state, ability, graft, visible, upgrades);
        bool Pass(CardUpgradeMaskRule mask, CardUpgradeMaskCard? card, bool allTypes = false) => CardUpgradeMaskModel.FilterCard(mask, card, allTypes);
        var impossible = new CardUpgradeMaskRule(cardType: "Spell", requireXCost: true, excludeXCost: true,
            requiredSizes: [1, 2], allowedPools: [["other"]]);
        Require(Pass(impossible, null), "Null-source card did not bypass every card filter.");
        Require(Pass(new(cardType: "Invalid", additionalCardTypes: ["Spell"]), Card()) &&
            !Pass(new(cardType: "Spell", additionalCardTypes: ["Invalid"]), Card()) &&
            Pass(new(cardType: "Spell", additionalCardTypes: ["Monster"]), Card()), "Primary/additional type rules differ.");
        Require(!Pass(new(subtypes: new(required: ["missing"])), Card()) &&
            Pass(new(subtypes: new(required: ["missing"])), Card(), true) &&
            !Pass(new(cardType: "Spell", subtypes: new(required: ["missing"])), Card(type: "Spell"), true),
            "All-subtypes marker bypassed the mask's required Monster-type validation.");
        Require(Pass(new(subtypes: new(excluded: ["A"], excludedOperator: 1)), Card()) &&
            !Pass(new(subtypes: new(excluded: ["A"], excludedOperator: 0)), Card()) &&
            !Pass(new(subtypes: new(excluded: ["A", "B"], excludedOperator: 1)), Card()), "Excluded OR must quantify over source entries.");
        Require(!Pass(new(statuses: new(required: [new("armor", 2)])), Card(statuses: [new("armor", 3)])) &&
            Pass(new(statuses: new(required: [new("armor", 3)])), Card(statuses: [new("armor", 2)])) &&
            !Pass(new(statuses: new(excluded: [new("armor", 2)])), Card(statuses: [new("armor", 3)])),
            "Asymmetric native status-stack comparisons were reversed.");
        var statusMask = new CardUpgradeMaskRule(statuses: new(required: [new("armor", 9)]));
        Require(Pass(statusMask, Card(type: "Spell")) &&
            !Pass(statusMask, Card(type: "Spell", main: [[new("armor", 2)], []])) &&
            Pass(statusMask, Card(type: "Spell", main: [[new("armor", 2)], [new("armor", 4)]])),
            "Non-spawner status filters did not independently check each main effect, including empty effects.");
        Require(!Pass(new(allowedPools: [["card"], ["other"]]), Card()) &&
            Pass(new(allowedPools: [[], ["card"]]), Card()) && !Pass(new(disallowedPools: [["card"]]), Card()),
            "Allowed pools must intersect, with empty pools ignored.");
        Require(!Pass(new(requiredSizes: [1, 2]), Card()) && Pass(new(requiredSizes: [2, 2], excludedSizes: [1]), Card()) &&
            Pass(new(requiredSizes: [1]), Card(type: "Spell")), "Size constraints must all match and only apply to spawners.");
        Require(Pass(new(minCost: 8, maxCost: 9), Card(cost: -5, xCost: true)) &&
            !Pass(new(minCost: 2.5f), Card(cost: 2)) && !Pass(new(requireXCost: true, excludeXCost: true), Card(xCost: true)),
            "X-cost range bypass or single-precision cost boundaries differ.");
        var ownedOnly = new CardUpgradeMaskRule(excludeIfHasUnitAbility: true, excludeIfHasGraftedEquipment: true,
            excludeIfHasAnyUpgrades: true, upgrades: new(required: ["known"]));
        Require(Pass(ownedOnly, Card(ability: true, graft: true, visible: 1)) &&
            !Pass(ownedOnly, Card(state: true, ability: true, upgrades: ["known"])) &&
            Pass(ownedOnly, Card(state: true, upgrades: ["known"])), "CardData incorrectly acquired CardState-only restrictions.");
        Require(Pass(new(targetMode: 3), Card(target: 0)) && Pass(new(targetMode: 3), Card(target: 3)) &&
            !Pass(new(targetMode: 1), Card(target: 3)), "Target flags must use native HasFlag containment, including zero flags.");
        Require(Pass(new(effects: new(required: ["cast", "trait-cast"])), Card(effects: ["main", "cast", "trait-cast"])),
            "OnCast and trait-upgrade effect names were lost.");
        var zeroCharacter = new CardUpgradeMaskCharacter(["A"], ["armor"], 2);
        Require(CardUpgradeMaskModel.FilterCharacter(new(cardType: "Spell", requireXCost: true,
            allowedPools: [["other"]], statuses: new(required: [new("armor", 999)])), zeroCharacter) &&
            !CardUpgradeMaskModel.FilterCharacter(new(statuses: new(excluded: [new("armor", -1)], excludedOperator: 1)), zeroCharacter),
            "Character filtering must include registered zero stacks and ignore card-only fields/status counts/operators.");
        var mutablePool = new[] { "card" }; var mutableSubtypes = new[] { "A" };
        var mask = new CardUpgradeMaskRule(allowedPools: [mutablePool], subtypes: new(required: mutableSubtypes));
        var card = Card(subtypes: mutableSubtypes);
        mutablePool[0] = "other"; mutableSubtypes[0] = "missing";
        string parent = JsonSerializer.Serialize(new { mask, card });
        Parallel.For(0, 32, _ => Require(Pass(mask, card), "Mask/card definitions shared mutable constructor inputs."));
        Require(JsonSerializer.Serialize(new { mask, card }) == parent, "Parallel filters mutated their parent definitions.");
        Console.WriteLine("UPGRADE-MASK-CHECKS PASS: native content semantics, CardData/CardState/null sources, zero-status characters, pools, X-cost, flags and 32 immutable branches.");
    }

    internal static void Native(string path)
    {
        using var document = ModelJson.ReadFixture(path);
        var root = document.RootElement;
        Require(root.GetProperty("Schema").GetInt32() == 2 && root.GetProperty("ContextUnchanged").GetBoolean() &&
            root.GetProperty("GameModuleMvid").GetString() == "8fb07b96-f4db-4d2b-884d-c00536d6ccf4" &&
            root.GetProperty("FrameBefore").ContentEquals(root.GetProperty("FrameAfter")) &&
            root.GetProperty("RngBefore").ContentEquals(root.GetProperty("RngAfter")), "Native mask calibration changed state/RNG/frame or game identity.");
        var cards = root.GetProperty("CardData").Deserialize<CardUpgradeMaskCard[]>()!;
        var owned = root.GetProperty("CardStates").Deserialize<CardUpgradeMaskCard[]>()!;
        var characters = root.GetProperty("Characters").Deserialize<CardUpgradeMaskCharacter[]>()!;
        bool allTypes = root.GetProperty("MonstersAreAllSubtypes").GetBoolean();
        var rows = root.GetProperty("Rows").EnumerateArray().Select(row => (
            Mask: row.GetProperty("Mask").Deserialize<CardUpgradeMaskRule>()!,
            Relic: row.GetProperty("OriginalRelicUpgradeMask").GetBoolean(),
            Null: row.GetProperty("NullCard").GetBoolean(),
            Cards: row.GetProperty("CardDataResults").Deserialize<bool[]>()!,
            Owned: row.GetProperty("CardStateResults").Deserialize<bool[]>()!,
            Characters: row.GetProperty("CharacterResults").Deserialize<bool[]>()!)).ToArray();
        Require(cards.Length == 668 && rows.Length == 62 && rows.Count(row => row.Relic) == 21 && owned.Length == 15 && characters.Length == 1 &&
            cards.Select(card => card.CardType).Distinct().Count() == 6, "Original mask/card/owned/character inventory coverage differs.");
        string parents = JsonSerializer.Serialize(new { cards, owned, characters, masks = rows.Select(row => row.Mask).ToArray() });
        int accepted = 0, rejected = 0;
        void CheckRow(int rowIndex, bool count)
        {
            var row = rows[rowIndex];
            Require(row.Cards.Length == cards.Length && row.Owned.Length == owned.Length && row.Characters.Length == characters.Length,
                "Incomplete native outcome matrix.");
            Require(CardUpgradeMaskModel.FilterCard(row.Mask, null, allTypes) == row.Null, "Native null-source filter differs.");
            for (int i = 0; i < cards.Length; i++) Compare(CardUpgradeMaskModel.FilterCard(row.Mask, cards[i], allTypes), row.Cards[i], "definition", i);
            for (int i = 0; i < owned.Length; i++) Compare(CardUpgradeMaskModel.FilterCard(row.Mask, owned[i], allTypes), row.Owned[i], "owned", i);
            for (int i = 0; i < characters.Length; i++) Compare(CardUpgradeMaskModel.FilterCharacter(row.Mask, characters[i], allTypes), row.Characters[i], "character", i);
            void Compare(bool predicted, bool actual, string kind, int index)
            {
                Require(predicted == actual, $"Native mask differs: {row.Mask.AssetKey}/{kind}/{index}, predicted={predicted}, actual={actual}.");
                if (count) { if (actual) accepted++; else rejected++; }
            }
        }
        for (int row = 0; row < rows.Length; row++) CheckRow(row, true);
        Require(accepted > 0 && rejected > 0, "Native mask matrix lacks both accepted and rejected cases.");
        var edges = root.GetProperty("EdgeCases").EnumerateArray().Select(edge => (
            Name: edge.GetProperty("Name").GetString()!, Kind: edge.GetProperty("Kind").GetString()!,
            Mask: edge.GetProperty("Mask").Deserialize<CardUpgradeMaskRule>()!,
            Card: edge.GetProperty("Card").Deserialize<CardUpgradeMaskCard>(),
            Character: edge.GetProperty("Character").Deserialize<CardUpgradeMaskCharacter>(),
            AllTypes: edge.GetProperty("MonstersAreAllSubtypes").GetBoolean(), Actual: edge.GetProperty("Actual").GetBoolean())).ToArray();
        Require(edges.Length == 58 && edges.Select(edge => edge.Name).Distinct().Count() == 58 &&
            edges.Count(edge => edge.AllTypes) == 2 && edges.Count(edge => edge.Kind == "Character") == 12 &&
            edges.Any(edge => edge.Name.StartsWith("registered-armor-0-") && edge.Actual) &&
            edges.Any(edge => edge.Name.StartsWith("registered-armor-0-") && !edge.Actual) &&
            edges.Any(edge => edge.Card?.VisibleUpgradeCount > 0) && edges.Any(edge => edge.Card?.HasGraftedEquipment == true),
            "Native filter boundary coverage differs.");
        string edgeParents = JsonSerializer.Serialize(edges);
        void CheckEdges()
        {
            foreach (var edge in edges)
            {
                Require(edge.Kind is "Card" or "Character" && (edge.Kind == "Card" || edge.Character != null), "Unknown or incomplete edge-case target.");
                bool predicted = edge.Kind == "Card" ? CardUpgradeMaskModel.FilterCard(edge.Mask, edge.Card, edge.AllTypes) :
                    CardUpgradeMaskModel.FilterCharacter(edge.Mask, edge.Character!, edge.AllTypes);
                Require(predicted == edge.Actual, $"Native filter boundary differs: {edge.Name}, predicted={predicted}, actual={edge.Actual}.");
            }
        }
        CheckEdges();
        Parallel.For(0, 32, _ => { for (int row = 0; row < rows.Length; row++) CheckRow(row, false); CheckEdges(); });
        Require(JsonSerializer.Serialize(new { cards, owned, characters, masks = rows.Select(row => row.Mask).ToArray() }) == parents,
            "Native matrix replay mutated parent inputs.");
        Require(JsonSerializer.Serialize(edges) == edgeParents, "Native boundary replay mutated parent inputs.");
        Console.WriteLine($"NATIVE-UPGRADE-MASK-CHECKS PASS: {rows.Length} original masks ({rows.Count(row => row.Relic)} relic-upgrade masks), {cards.Length} original cards, {owned.Length} owned cards, {characters.Length} characters; {accepted + rejected + rows.Length} exact matrix queries, {accepted} accepted/{rejected} rejected, {edges.Length} native boundary queries, 32 immutable branches.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
