using Irony.Parsing;
using Irony.Parsing.SpecialActionHints;

namespace Irony.Tests.TokenPreviewResolution {

    [Language("Grammar with conflicts #4", "1.1", "Test conflict grammar with conflicts and hints: hints are added to non-terminals.")]
    public sealed class ConflictGrammarWithHintsOnTerms : Grammar {

        public ConflictGrammarWithHintsOnTerms()
            : base(true) {
            var name = new IdentifierTerminal("id");

            var stmt = new NonTerminal("Statement");
            var stmtList = new NonTerminal("StatementList");
            var fieldModifier = new NonTerminal("fieldModifier");
            var propModifier = new NonTerminal("propModifier");
            var methodModifier = new NonTerminal("methodModifier");
            var fieldModifierList = new NonTerminal("fieldModifierList");
            var propModifierList = new NonTerminal("propModifierList");
            var methodModifierList = new NonTerminal("methodModifierList");
            var fieldDef = new NonTerminal("fieldDef");
            var propDef = new NonTerminal("propDef");
            var methodDef = new NonTerminal("methodDef");

            //Rules
            Root = stmtList;
            stmtList.Rule = MakePlusRule(stmtList, stmt);
            stmt.Rule = fieldDef | propDef | methodDef;
            fieldDef.Rule = fieldModifierList + name + name + ";";
            propDef.Rule = propModifierList + name + name + "{" + "}";
            methodDef.Rule = methodModifierList + name + name + "(" + ")" + "{" + "}";
            fieldModifierList.Rule = MakeStarRule(fieldModifierList, fieldModifier);
            propModifierList.Rule = MakeStarRule(propModifierList, propModifier);
            methodModifierList.Rule = MakeStarRule(methodModifierList, methodModifier);

            fieldModifier.Rule = ToTerm("public") | "private" | "readonly" | "volatile";
            propModifier.Rule = ToTerm("public") | "private" | "readonly" | "override";
            methodModifier.Rule = ToTerm("public") | "private" | "override";

            // conflict resolution
            var fieldHint = new TokenPreviewHint(PreferredActionType.Reduce, ";", "(", "{");
            fieldModifier.AddHintToAll(fieldHint);
            fieldModifierList.AddHintToAll(fieldHint);
            var propHint = new TokenPreviewHint(PreferredActionType.Reduce, "{", ";", "(");
            propModifier.AddHintToAll(propHint);
            propModifierList.AddHintToAll(propHint);

            MarkReservedWords("public", "private", "readonly", "volatile", "override");
        }

    }

}
