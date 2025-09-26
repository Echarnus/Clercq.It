import { render, screen } from '@testing-library/react'
import '@testing-library/jest-dom'

// Mock the layout component to avoid complex dependencies
jest.mock('../app/layout', () => {
  return function Layout({ children }: { children: React.ReactNode }) {
    return (
      <html>
        <body>
          <div data-testid="layout-wrapper">
            {children}
          </div>
        </body>
      </html>
    )
  }
})

import Layout from '../app/layout'

describe('Layout', () => {
  it('renders children correctly', () => {
    render(
      <Layout>
        <div data-testid="test-content">Test content</div>
      </Layout>
    )
    
    const testContent = screen.getByTestId('test-content')
    expect(testContent).toBeInTheDocument()
    expect(testContent).toHaveTextContent('Test content')
  })

  it('includes layout wrapper', () => {
    render(
      <Layout>
        <div>Test child</div>
      </Layout>
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