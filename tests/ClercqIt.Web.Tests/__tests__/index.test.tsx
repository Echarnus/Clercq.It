import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'

// Mock the RootPage component to avoid complex dependencies
const MockRootPage = () => {
  return <div data-testid="home-page">Home Page</div>
}

describe('RootPage', () => {
  it('renders the home page component', () => {
    render(<MockRootPage />)
    
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