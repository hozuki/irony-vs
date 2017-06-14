using Irony.Parsing;

namespace Irony.Tests.TokenPreviewResolution {

    [Language("Grammar with conflicts #2", "1.1", "Conflict grammar with hints added to productions.")]
    public sealed class ConflictGrammarWithHintsInRules : Grammar {

        public ConflictGrammarWithHintsInRules()
            : base(true) {
            var name = new IdentifierTerminal("id");

            var definition = new NonTerminal("definition");
            var fieldDef = new NonTerminal("fieldDef");
            var propDef = new NonTerminal("propDef");
            var fieldModifier = new NonTerminal("fieldModifier");
            var propModifier = new NonTerminal("propModifier");

            definition.Rule = fieldDef | propDef;
            fieldDef.Rule = fieldModifier + name + name + ";";
            propDef.Rule = propModifier + name + name + "{" + "}";
            var fieldHint = ReduceIf(";", comesBefore: "{");
            fieldModifier.Rule = "public" + fieldHint | "private" + fieldHint | "readonly";
            propModifier.Rule = ToTerm("public") | "private" | "override";

            Root = definition;
        }

    }

}
