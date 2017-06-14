using Irony.Parsing;

namespace Irony.Tests.TokenPreviewResolution {

    [Language("Grammar with conflicts, no hints", "1.1", "Grammar with conflicts, no hints.")]
    public sealed class ConflictGrammarNoHints : Grammar {

        public ConflictGrammarNoHints()
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

            // That's the key of the problem: 3 modifiers have common members
            //   so parser automaton has hard time deciding which modifiers list to produce - 
            //   is it a field, prop or method we are beginning to parse?
            fieldModifier.Rule = ToTerm("public") | "private" | "readonly" | "volatile";
            propModifier.Rule = ToTerm("public") | "private" | "readonly" | "override";
            methodModifier.Rule = ToTerm("public") | "private" | "override";

            MarkReservedWords("public", "private", "readonly", "volatile", "override");
        }

    }

}
