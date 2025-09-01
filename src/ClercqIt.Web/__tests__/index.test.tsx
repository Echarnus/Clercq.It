import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'
import RootPage from '../app/page'

// Mock the home page component since it might have complex dependencies
jest.mock('../app/home/page', () => {
  return function HomePage() {
    return <div data-testid="home-page">Home Page</div>
  }
})

describe('RootPage', () => {
  it('renders the home page component', () => {
    render(<RootPage />)
    
    const homePageElement = screen.getByTestId('home-page')
    expect(homePageElement).toBeInTheDocument()
    expect(homePageElement).toHaveTextContent('Home Page')
  })
})

// Simple smoke test for the application
describe('Application Environment', () => {
  it('should have proper environment setup', () => {
    expect(process.env.NODE_ENV).toBeDefined()
  })
})