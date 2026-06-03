"use client";

import { useEffect } from "react";
import { Link, Navigate, useParams } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import { useTranslation } from "react-i18next";
import { PublicLayout, Section } from "@/components/public/PublicLayout";
import { FinalCta } from "@/components/public/PublicComponents";
import { PublicSeo } from "@/lib/public-seo";
import { guidesForLocale, supportedGuideLocales } from "@/content/guides/registry";
import { guidePath, guidesIndexPath, localeFromParam, ORIGIN } from "@/content/guides/routing";
import { DEFAULT_LOCALE, type Locale } from "@/i18n/languages";

interface IndexCopy {
  title: string;
  description: string;
  h1: string;
  lead: string;
  cta: string;
}

const COPY: Partial<Record<Locale, IndexCopy>> = {
  en: {
    title: "Used Car Buying Guides: Model Risks & What to Check | AutoVerdict",
    description:
      "Model-by-model used car buying guides — what to check, what to ask the seller, and the risks to verify before you view or pay for a car.",
    h1: "Used car buying guides",
    lead: "Practical, model-specific checklists for buying used. Each guide covers what to verify, the questions to ask the seller, and an inspection checklist — then run your own listing for a free, personalised risk report.",
    cta: "Read the guide",
  },
  pl: {
    title: "Poradniki zakupu używanych aut: ryzyka modeli i co sprawdzić | AutoVerdict",
    description:
      "Poradniki zakupu używanych samochodów model po modelu — co sprawdzić, o co zapytać sprzedającego i jakie ryzyka zweryfikować przed obejrzeniem auta.",
    h1: "Poradniki zakupu używanych aut",
    lead: "Praktyczne listy kontrolne dla konkretnych modeli. Każdy poradnik mówi, co zweryfikować, o co zapytać sprzedającego i jak przeprowadzić oględziny — a potem możesz przeanalizować własne ogłoszenie w bezpłatnym raporcie ryzyka.",
    cta: "Przeczytaj poradnik",
  },
  de: {
    title: "Gebrauchtwagen-Kaufratgeber: Modellrisiken & worauf achten | AutoVerdict",
    description:
      "Gebrauchtwagen-Kaufratgeber Modell für Modell — was zu prüfen ist, was Sie den Verkäufer fragen sollten und welche Risiken Sie vor Besichtigung oder Kauf abklären.",
    h1: "Gebrauchtwagen-Kaufratgeber",
    lead: "Praktische, modellspezifische Checklisten für den Gebrauchtwagenkauf. Jeder Ratgeber zeigt, was zu prüfen ist, welche Fragen Sie dem Verkäufer stellen sollten, und eine Checkliste für die Besichtigung — danach können Sie Ihr eigenes Inserat in einem kostenlosen Risikobericht analysieren.",
    cta: "Ratgeber lesen",
  },
  fr: {
    title: "Guides d'achat de voitures d'occasion : risques par modèle | AutoVerdict",
    description:
      "Guides d'achat de voitures d'occasion modèle par modèle — quoi vérifier, quoi demander au vendeur et les risques à contrôler avant de voir ou d'acheter une voiture.",
    h1: "Guides d'achat de voitures d'occasion",
    lead: "Des check-lists pratiques et spécifiques à chaque modèle pour acheter d'occasion. Chaque guide indique quoi vérifier, les questions à poser au vendeur et une check-list d'inspection — puis analysez votre propre annonce dans un rapport de risque gratuit.",
    cta: "Lire le guide",
  },
  uk: {
    title: "Посібники з купівлі вживаних авто: ризики за моделями | AutoVerdict",
    description:
      "Посібники з купівлі вживаних авто модель за моделлю — що перевірити, про що запитати продавця та які ризики з'ясувати перед оглядом чи купівлею.",
    h1: "Посібники з купівлі вживаних авто",
    lead: "Практичні чек-листи для конкретних моделей. Кожен посібник підкаже, що перевірити, які питання поставити продавцю, і дасть чек-лист для огляду — після чого ви можете проаналізувати власне оголошення у безкоштовному звіті про ризики.",
    cta: "Читати посібник",
  },
};

export default function GuidesIndexPage() {
  const params = useParams<{ locale?: string }>();
  const locale = localeFromParam(params.locale);
  const { i18n } = useTranslation();

  useEffect(() => {
    if (locale && i18n.language !== locale) i18n.changeLanguage(locale);
  }, [locale, i18n]);

  if (locale === null) return <Navigate to="/guides" replace />;
  // Only serve a localized index for locales we actually have guides in.
  if (locale !== DEFAULT_LOCALE && !supportedGuideLocales().includes(locale)) {
    return <Navigate to="/guides" replace />;
  }

  const copy = COPY[locale] ?? COPY.en!;
  const guides = guidesForLocale(locale);
  const path = guidesIndexPath(locale);

  const alternates = supportedGuideLocales().map((loc) => ({
    locale: loc,
    path: guidesIndexPath(loc),
  }));

  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "CollectionPage",
    name: copy.h1,
    description: copy.description,
    url: `${ORIGIN}${path}`,
    inLanguage: locale,
    mainEntity: {
      "@type": "ItemList",
      itemListElement: guides.map((guide, index) => ({
        "@type": "ListItem",
        position: index + 1,
        name: `${guide.make} ${guide.model}`,
        url: `${ORIGIN}${guidePath(guide.slug, locale)}`,
      })),
    },
  };

  return (
    <PublicLayout>
      <PublicSeo
        path={path}
        title={copy.title}
        description={copy.description}
        ogTitle={copy.h1}
        locale={locale}
        alternates={alternates}
        jsonLd={jsonLd}
      />
      <main>
        <Section className="border-t-0 bg-[#070A0F]">
          <h1 className="max-w-3xl text-4xl font-extrabold text-white md:text-6xl">{copy.h1}</h1>
          <p className="mt-6 max-w-3xl text-lg leading-8 text-slate-300">{copy.lead}</p>
        </Section>

        <Section>
          <div className="grid gap-4 md:grid-cols-2">
            {guides.map((guide) => (
              <Link
                key={guide.slug}
                to={guidePath(guide.slug, locale)}
                className="group rounded-2xl border border-slate-400/10 bg-[#101722] p-6 transition-colors hover:border-[#7C9CFF]/40"
              >
                <p className="text-xs font-bold uppercase tracking-wide text-[#AFC0FF]">{guide.years}</p>
                <h2 className="mt-2 text-xl font-bold text-white">{guide.make} {guide.model}</h2>
                <p className="mt-3 text-sm leading-6 text-slate-400">{guide.intro}</p>
                <span className="mt-4 inline-flex items-center gap-1 text-sm font-semibold text-[#7C9CFF]">
                  {copy.cta}
                  <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" />
                </span>
              </Link>
            ))}
          </div>
        </Section>

        <Section><FinalCta /></Section>
      </main>
    </PublicLayout>
  );
}
