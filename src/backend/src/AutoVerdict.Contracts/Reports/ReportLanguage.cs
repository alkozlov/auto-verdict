namespace AutoVerdict.Contracts.Reports;

public sealed record ReportLanguage(
    string Locale,
    string EnglishName,
    string NativeName,
    string BuyVerdict,
    string CautionVerdict,
    string AvoidVerdict,
    string Disclaimer,
    IReadOnlyList<string> RequiredHeadings)
{
    public static readonly ReportLanguage English = new(
        "en",
        "English",
        "English",
        "Buy",
        "Buy with caution",
        "Avoid",
        "*Disclaimer: AutoVerdict provides AI-assisted preliminary screening only. Always verify documents and arrange an independent inspection before purchasing.*",
        [
            "# Verdict",
            "# Key Risks",
            "## Technical Risks",
            "## Listing Risks",
            "## Deal Risks",
            "# Missing Information",
            "# Questions for the Seller",
            "# Inspection Checklist",
            "# Vehicle Facts",
            "# Estimated Costs",
            "# Summary",
        ]);

    private static readonly ReportLanguage Polish = new(
        "pl",
        "Polish",
        "Polski",
        "Kup",
        "Kupuj ostrożnie",
        "Unikaj",
        "*Zastrzeżenie: AutoVerdict zapewnia wyłącznie wstępną analizę wspieraną przez AI. Przed zakupem zawsze zweryfikuj dokumenty i zorganizuj niezależne oględziny.*",
        [
            "# Werdykt",
            "# Kluczowe ryzyka",
            "## Ryzyka techniczne",
            "## Ryzyka ogłoszenia",
            "## Ryzyka transakcji",
            "# Brakujące informacje",
            "# Pytania do sprzedawcy",
            "# Lista kontrolna oględzin",
            "# Fakty o pojeździe",
            "# Szacowane koszty",
            "# Podsumowanie",
        ]);

    private static readonly ReportLanguage German = new(
        "de",
        "German",
        "Deutsch",
        "Kaufen",
        "Mit Vorsicht kaufen",
        "Vermeiden",
        "*Haftungsausschluss: AutoVerdict bietet nur eine KI-gestützte Vorprüfung. Prüfen Sie vor dem Kauf immer die Dokumente und organisieren Sie eine unabhängige Inspektion.*",
        [
            "# Urteil",
            "# Zentrale Risiken",
            "## Technische Risiken",
            "## Risiken der Anzeige",
            "## Risiken des Geschäfts",
            "# Fehlende Informationen",
            "# Fragen an den Verkäufer",
            "# Checkliste für die Inspektion",
            "# Fahrzeugdaten",
            "# Geschätzte Kosten",
            "# Zusammenfassung",
        ]);

    private static readonly ReportLanguage Ukrainian = new(
        "uk",
        "Ukrainian",
        "Українська",
        "Купувати",
        "Купувати обережно",
        "Уникати",
        "*Відмова від відповідальності: AutoVerdict надає лише попередню перевірку за допомогою AI. Перед купівлею завжди перевіряйте документи та організовуйте незалежний огляд.*",
        [
            "# Вердикт",
            "# Ключові ризики",
            "## Технічні ризики",
            "## Ризики оголошення",
            "## Ризики угоди",
            "# Відсутня інформація",
            "# Запитання до продавця",
            "# Контрольний список огляду",
            "# Факти про автомобіль",
            "# Орієнтовні витрати",
            "# Підсумок",
        ]);

    private static readonly ReportLanguage French = new(
        "fr",
        "French",
        "Français",
        "Acheter",
        "Acheter avec prudence",
        "Éviter",
        "*Avertissement : AutoVerdict fournit uniquement une pré-évaluation assistée par IA. Vérifiez toujours les documents et organisez une inspection indépendante avant l'achat.*",
        [
            "# Verdict",
            "# Risques clés",
            "## Risques techniques",
            "## Risques liés à l'annonce",
            "## Risques de transaction",
            "# Informations manquantes",
            "# Questions pour le vendeur",
            "# Liste de contrôle d'inspection",
            "# Données du véhicule",
            "# Coûts estimés",
            "# Résumé",
        ]);

    public string VerdictLabels => $"{BuyVerdict}, {CautionVerdict}, or {AvoidVerdict}";

    public string MapVerdict(string verdict) =>
        verdict.Trim().ToLowerInvariant() switch
        {
            "buy" => BuyVerdict,
            "buy with caution" => CautionVerdict,
            "avoid" => AvoidVerdict,
            _ => CautionVerdict,
        };

    public static bool IsSupported(string? locale) => TryResolve(locale, out _);

    public static ReportLanguage Resolve(string? locale) =>
        TryResolve(locale, out var language) ? language : English;

    public static bool TryResolve(string? locale, out ReportLanguage language)
    {
        language = English;
        if (string.IsNullOrWhiteSpace(locale))
            return true;

        language = locale.Trim().ToLowerInvariant() switch
        {
            "en" => English,
            "pl" => Polish,
            "de" => German,
            "uk" => Ukrainian,
            "fr" => French,
            _ => English,
        };

        return locale.Trim().ToLowerInvariant() is "en" or "pl" or "de" or "uk" or "fr";
    }
}
