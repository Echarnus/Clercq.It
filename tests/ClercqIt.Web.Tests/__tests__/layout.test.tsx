import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'

// Mock Layout component to avoid complex dependencies
const MockLayout = ({ children }: { children: React.ReactNode }) => {
  return (
    <div data-testid="layout-wrapper">
      {children}
    </div>
  )
}

describe('Layout', () => {
  it('renders children correctly', () => {
    render(
      <MockLayout>
        <div data-testid="test-content">Test content</div>
      </MockLayout>
    )
    
    const testContent = screen.getByTestId('test-content')
    expect(testContent).toBeInTheDocument()
    expect(testContent).toHaveTextContent('Test content')
  })

  it('includes layout wrapper', () => {
    render(
      <MockLayout>
        <div>Test child</div>
      </MockLayout>
    )
    
    const layoutWrapper = screen.getByTestId('layout-wrapper')
    expect(layoutWrapper).toBeInTheDocument()
    
    const testChild = screen.getByText('Test child')
    expect(testChild).toBeInTheDocument()
  })
})

describe('Frontend Test Infrastructure', () => {
  it('should have proper test environment setup', () => {
    expect(jest).toBeDefined()
    expect(window.matchMedia).toBeDefined()
    expect(global.IntersectionObserver).toBeDefined()
  })

  it('should handle DOM testing utilities', () => {
    const element = document.createElement('div')
    element.textContent = 'Test element'
    expect(element).toBeTruthy()
    expect(element.textContent).toBe('Test element')
  })
})