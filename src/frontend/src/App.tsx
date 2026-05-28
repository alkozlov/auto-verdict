import { Routes, Route, Navigate } from 'react-router-dom'
import HomePage from './app/page'
import AuthCallback from './app/auth/callback/page'
import GarageLayout from './app/garage/layout'
import CheckPage from './app/garage/check/page'
import ReportsPage from './app/garage/reports/page'
import ReportPage from './app/garage/reports/[id]/page'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<HomePage />} />
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
