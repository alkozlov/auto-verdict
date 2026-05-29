# SEO Technical Debt

Current public-page metadata is client-side because the frontend is a Vite SPA.
This is acceptable only for the MVP UI readiness pass.

Before serious SEO work, implement one of these options:

1. Add static prerendering for public routes.
2. Split the app into a static/SSR marketing site for public pages and a Vite SPA for the authenticated garage.
3. Move public pages back to an SSR/SSG-compatible framework if needed.

The public page components and route-specific metadata are intentionally isolated so they can be prerendered later without rewriting the whole UI.
