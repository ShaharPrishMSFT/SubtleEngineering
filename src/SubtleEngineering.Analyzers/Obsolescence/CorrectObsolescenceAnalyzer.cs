namespace SubtleEngineering.Analyzers.Obsolescence
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using SubtleEngineering.Analyzers.Decorators;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CorrectObsolescenceAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1040 = 0;

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticsDetails.Obsolescence.HideObsoleteElementsId,
                "Obsoleted elements should be hidden from intellisense.",
                "Element '{0}' is marked as being Obsolete - it's probably a good idea to hide it from Intellisense. Add the [EditorBrowsable(EditorBrowsableState.Never)] to hide it, or explicitly add the attribute saying you want it to show up.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register the symbol action for all symbol kinds that can be marked as obsolete
            context.RegisterSymbolAction(AnalyzeSymbol,
                SymbolKind.NamedType,   // For classes, structs, interfaces, enums, delegates
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Field,
                SymbolKind.Event);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol;

            // Check if the symbol is marked as Obsolete
            var obsoleteAttribute = symbol.GetAttributes().FirstOrDefault(attr =>
                attr.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute");

            if (obsoleteAttribute == null)
            {
                return; // Symbol is not obsolete; no action needed
            }

            // Check if the symbol has an EditorBrowsableAttribute
            var hasEditorBrowsableAttribute = symbol.GetAttributes().Any(attr =>
                attr.AttributeClass?.ToDisplayString() == "System.ComponentModel.EditorBrowsableAttribute");

            if (hasEditorBrowsableAttribute)
            {
                return; // Symbol explicitly specifies EditorBrowsable; no action needed
            }

            // Report a diagnostic if the symbol is obsolete but not hidden from Intellisense
            var diagnostic = Diagnostic.Create(Rules[0], symbol.Locations[0], symbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
