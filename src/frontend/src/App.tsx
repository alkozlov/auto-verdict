import { Routes, Route, Navigate } from 'react-router-dom'
import HomePage from './app/page'
import HowItWorksPage from './app/how-it-works/page'
import SampleReportPage from './app/sample-report/page'
import PricingPage from './app/pricing/page'
import PrivacyPage from './app/privacy/page'
import TermsPage from './app/terms/page'
import ContactPage from './app/contact/page'
import GuidesIndexPage from './app/guides/page'
import GuidePage from './app/guides/[slug]/page'
import AuthCallback from './app/auth/callback/page'
import GarageLayout from './app/garage/layout'
import CheckPage from './app/garage/check/page'
import ReportsPage from './app/garage/reports/page'
import ReportPage from './app/garage/reports/[id]/page'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route path="/how-it-works" element={<HowItWorksPage />} />
      <Route path="/sample-report" element={<SampleReportPage />} />
      <Route path="/pricing" element={<PricingPage />} />
      <Route path="/privacy" element={<PrivacyPage />} />
      <Route path="/terms" element={<TermsPage />} />
      <Route path="/contact" element={<ContactPage />} />
      <Route path="/guides" element={<GuidesIndexPage />} />
      <Route path="/guides/:slug" element={<GuidePage />} />
      <Route path="/:locale/guides" element={<GuidesIndexPage />} />
      <Route path="/:locale/guides/:slug" element={<GuidePage />} />
      <Route path="/auth/callback" element={<AuthCallback />} />
      <Route path="/dashboard" element={<Navigate to="/" replace />} />
      <Route path="/garage" element={<GarageLayout />}>
        <Route index element={<Navigate to="check" replace />} />
        <Route path="check" element={<CheckPage />} />
        <Route path="reports" element={<ReportsPage />} />
        <Route path="reports/:id" element={<ReportPage />} />
      </Route>
    </Routes>
  )
}
