import { Routes, Route } from 'react-router-dom'
import PartyList from './PartyList'
import PartyForm from './PartyForm'
import PartyDetail from './PartyDetail'

const PartiesPage: React.FC = () => {
  return (
    <Routes>
      <Route path="/" element={<PartyList />} />
      <Route path="/new" element={<PartyForm />} />
      <Route path="/:id" element={<PartyDetail />} />
      <Route path="/:id/edit" element={<PartyForm />} />
    </Routes>
  )
}

export default PartiesPage
