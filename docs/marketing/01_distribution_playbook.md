# AutoVerdict — Manual Distribution Playbook (Workstream C)

**Goal of this phase:** get your first ~50 real users and qualitative feedback to validate
the core hypothesis (PRD §16). Not revenue. Conversations and feedback > raw signups.

**Constraints this is designed for:** €0 ad budget, ~2–4 hours/week, pan‑European audience,
payments not yet live. English‑led, because it's the widest single net and the language you
can QA. Poland is your strongest market (Otomoto auto‑crawl), so weight effort there when in doubt.

**Time budget:** this whole playbook fits in 2–4h/week. See the weekly cadence at the end.

---

## 1. The core mechanic: "give the verdict, then offer the tool"

Do **not** post links and ask people to sign up. Reddit and Facebook groups punish that, and
it doesn't build trust. Instead, run the loop that turns your product into the proof:

1. **Find** a post where someone shares a used‑car listing and asks "is this a good deal / is
   this legit / what should I watch out for?"
2. **Run it through AutoVerdict yourself** (your own account, your free/credit — the stranger
   never has to sign up to receive value).
3. **Reply with a genuinely useful, condensed verdict** — 4–6 concrete, specific points pulled
   from the report. Specific to *their* car, not generic.
4. **Soft CTA**, only where the sub/group allows it: "I built a small tool that generates this —
   the first check is free if you want to run your own listing." Disclose that you built it.

Why this works: you demonstrate the product live, the value is undeniable and specific, and the
person (plus every lurker reading the thread) sees exactly what they'd get. Lurkers convert more
than the original poster.

**The unfair advantage:** you can deliver value to people who never sign up, which removes all
friction and never feels like spam. Even if they never click, the thread is now a permanent,
searchable demonstration of your product's quality.

---

## 2. Where to show up (target list)

Check each community's self‑promotion rules before posting. They vary a lot. Rule of thumb:
**value‑first comments are welcome almost everywhere; standalone promo posts are only OK in
launch/startup communities.**

### Reddit — buyer-intent (your main hunting ground)
Sort by *New* and look for listing-opinion posts.
- r/whatcarshouldIbuy — very active, people literally post listings asking for opinions.
- r/UsedCars
- r/MechanicAdvice — people post "should I buy this" with photos.
- r/askcarsales — strict, pros only; **read-only / learn the objections**, don't pitch.

### Reddit — country & regional (pan-European reach)
- Poland: r/Polska, r/poland, r/MotoPolska (if active)
- Germany: r/de, r/germany, r/Autos (DE)
- France: r/france, r/conduite
- Ukraine: r/ukraina, r/Ukraine
- UK/EN: r/AskUK, r/CarTalkUK, r/UKPersonalFinance (car-buying threads)

### Reddit — model owner subs (highest-intent, lowest competition)
When you spot a recurring model in listings, these are gold:
- r/BMW, r/Volkswagen, r/Volvo, r/Audi, r/Mercedes_Benz, r/ToyotaTacoma-style model subs, etc.
- People here *love* talking about model-specific weak points — exactly your report's
  "model-specific things to verify" section.

### Reddit — launch / build-in-public (for standalone posts)
These *allow* "I built this" posts. Use 1–2 per week max, with a real story.
- r/SideProject, r/SaaS, r/EntrepreneurRideAlong, r/microsaas
- r/InternetIsBeautiful (only once, only if the site is polished)
- r/alphaandbetausers, r/roastmystartup (great for feedback, your actual goal)

### Facebook groups (find via search; PL/DE strongest)
Search patterns inside Facebook:
- `"skup aut" / "kupię auto" / "Otomoto" / "[city] sprzedam samochód"` (PL)
- `"Gebrauchtwagen kaufen" / "Auto kaufen [Stadt]"` (DE)
- Brand owner groups: `"VW Passat właściciele"`, `"BMW E90 Polska"`, etc.
Owner groups are friendlier to helpful contributions than buy/sell marketplaces.

### Product Hunt
Save for **after** the landing page polish + the SEO foundation (Workstream A) is done. One good
launch. Not now.

### Forums (slow but durable)
Country car forums rank in Google for years. Find the top 1–2 per country and answer
buying-advice threads with a signature link. Lower volume, compounding value.

---

## 3. Templates

Tone rules for all of them: specific > generic, humble, never claim certainty ("this *may*
indicate…", per PRD §13), always disclose you built the tool, never DM-spam.

### Template A — Helpful reply on a listing-opinion post (your bread and butter)

> Had a look at this one. A few things I'd want clarified before you go see it:
>
> - **Service history** — the ad doesn't mention a full service book. For a [model] at [km] km
>   that's the difference between a sound buy and an expensive one; ask for stamped records or invoices.
> - **[Model-specific point]** — [year] [model]s with the [engine] are known for [specific issue].
>   Ask when/if [component] was done.
> - **Accident-free claim** — it's stated but not backed by anything. Ask for the VIN and run it
>   through a history check yourself.
> - **Ownership/import** — [n] owners in [n] years / imported is worth a question about why.
>
> Questions I'd send the seller first: [2–3 concrete questions]. If it checks out, get a
> pre-purchase inspection before paying.

*(Where the sub allows promo, add:)*

> Full disclosure — I built a free tool that generates exactly this kind of pre-purchase
> breakdown from a listing; happy to run yours and share the full report if useful.

### Template B — DM / follow-up when someone says "yes, run it"

> Here's the full report for your [model]: [paste condensed report or link].
> Two things I'd genuinely value if you have a minute: (1) was anything in here useful/wrong,
> and (2) would you have paid a couple of euros for this before a 200 km drive to view a car?
> No pressure — feedback helps me more than a signup right now.

*(The feedback questions are the point — you're validating, not selling.)*

### Template C — "I built this" launch/feedback post (startup subs only)

> **I built a tool that flags the risks in a used-car listing before you contact the seller**
>
> Inexperienced buyers (me included) miss the warning signs in used-car ads — vague service
> history, unsupported accident-free claims, import/ownership oddities, model-specific weak spots.
>
> AutoVerdict takes a listing (paste the text, a link, or screenshots) and returns a structured
> risk report: risk level, missing info, red flags, questions to ask the seller, an inspection
> checklist, and model-specific things to verify. First analysis is free, no card.
>
> It's an early MVP and I'm specifically looking for feedback: is the report actually useful,
> what's missing, would you trust it? Roast away. [link]

### Template D — Facebook owner-group value post

> If anyone here is shopping for a used [model], the things most worth checking before you travel
> to view one: [3 model-specific points]. Built a free tool that turns a listing into this kind of
> checklist automatically — drop a link/screenshots in the comments and I'll run it for you.

---

## 4. Rules of engagement (so you don't get banned)

- **9:1 ratio** — at least nine genuinely helpful, no-pitch contributions for every promotional
  mention. Easiest way to stay welcome.
- **Read each community's rules** on self-promotion before your first post. When unsure, give value
  with no link and let people ask.
- **Always disclose** you're the maker. "I built this" is fine; pretending to be a happy user is not.
- **Never cold-DM** people who didn't ask. Reply in-thread; only DM when they opt in.
- **One account, real history** — don't create burner accounts; established accounts aren't filtered.
- **Don't copy-paste** the same comment. Each reply must be specific to that car or it reads as spam
  (and gets removed).

---

## 5. Weekly cadence (fits 2–4 hours)

| Day | ~Time | Action |
|-----|-------|--------|
| Mon | 45 min | Scan r/whatcarshouldIbuy + 2 country subs (sort *New*); run 2–3 listings, post Template A replies. |
| Wed | 45 min | Same in model-owner subs + 1 Facebook group. Follow up on anyone who engaged (Template B). |
| Fri | 30 min | One Template C/D post in a startup or owner community. Log feedback. |
| Any | 30 min | Reply to every comment/DM you got. Conversations are the goal. |

Skip a slot rather than spam to fill it. Quality of each reply is what compounds.

---

## 6. Tracking (lightweight, since the goal is validation)

Keep a simple sheet (Google Sheets) with one row per outreach:

`date | community | car/model | link to your comment | response? | signed up? | feedback notes`

Also:
- Put a `?utm_source=reddit` / `utm_source=fb` param on any link you share, so signups can be
  attributed later when analytics is in place.
- Every Friday, write 3 lines: how many helpful replies, how many "yes run it", what feedback
  themes recurred. That weekly note is your validation signal — watch for the same pain/request
  showing up repeatedly.

**What "validated" looks like after ~4 weeks:** people proactively asking you to run their
listing, repeated "I'd have paid for this" comments, and a recurring most-wanted feature. If
instead you get politeness but no pull, that's a signal to revisit positioning before investing
in the SEO build.

---

## Next workstreams (queued)

- **A — SEO technical foundation** (prerender public routes, robots.txt, sitemap, hreflang).
  Code task; unlocks all organic search. See `seo-technical-debt.md`.
- **B — Programmatic content engine** (model/year risk pages generated via the existing AI
  pipeline, English first, then translated into PL/DE/FR/UK). Depends on A.
