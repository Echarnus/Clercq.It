const path = require('path')

// Mock Next.js configuration since we're not using the actual Next.js instance
const customJestConfig = {
  setupFilesAfterEnv: ['<rootDir>/jest.setup.js'],
  testEnvironment: 'jsdom',
  testPathIgnorePatterns: ['<rootDir>/.next/', '<rootDir>/node_modules/'],
  moduleNameMapper: {
    '^@/(.*)$': '<rootDir>/../../src/ClercqIt.Web/$1',
    '^~/(.*)$': '<rootDir>/../../src/ClercqIt.Web/$1',
  },
  rootDir: '.',
  testMatch: ['**/__tests__/**/*.(js|jsx|ts|tsx)', '**/*.(test|spec).(js|jsx|ts|tsx)'],
  transform: {
    '^.+\\.(js|jsx|ts|tsx)$': ['babel-jest', { presets: ['next/babel'] }],
  },
  moduleFileExtensions: ['ts', 'tsx', 'js', 'jsx', 'json'],
  collectCoverageFrom: [
    '../../src/ClercqIt.Web/**/*.{ts,tsx}',
    '!../../src/ClercqIt.Web/**/*.d.ts',
    '!../../src/ClercqIt.Web/.next/**',
  ],
}

module.exports = customJestConfig