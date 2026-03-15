import { Routes, Route } from 'react-router-dom'
import BookList from './BookList'
import BookForm from './BookForm'
import BookDetail from './BookDetail'

const BooksPage: React.FC = () => {
  return (
    <Routes>
      <Route path="/" element={<BookList />} />
      <Route path="/new" element={<BookForm />} />
      <Route path="/:id" element={<BookDetail />} />
      <Route path="/:id/edit" element={<BookForm />} />
    </Routes>
  )
}

export default BooksPage
