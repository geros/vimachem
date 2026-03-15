import { Routes, Route } from 'react-router-dom'
import PartyList from './PartyList'
import PartyForm from './PartyForm'

const PartiesPage: React.FC = () => {
  return (
    <Routes>
      <Route path="/" element={<PartyList />} />
      <Route path="/new" element={<PartyForm />} />
      <Route path="/:id" element={<PartyForm />} />
    </Routes>
  )
}

export default PartiesPage
