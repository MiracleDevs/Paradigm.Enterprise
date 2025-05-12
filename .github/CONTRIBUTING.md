# Contributing to Paradigm Framework

Thank you for your interest in contributing to the Paradigm Framework! This document provides guidelines and instructions for contributing to this project.

## Table of Contents

- [Contributing to Paradigm Framework](#contributing-to-paradigm-framework)
  - [Table of Contents](#table-of-contents)
  - [Code of Conduct](#code-of-conduct)
  - [Getting Started](#getting-started)
  - [Development Workflow](#development-workflow)
  - [Pull Request Process](#pull-request-process)
  - [RFC Process](#rfc-process)
  - [Coding Standards](#coding-standards)
  - [Testing](#testing)
  - [Documentation](#documentation)
  - [Issue Reporting](#issue-reporting)

## Code of Conduct

This project adheres to a Code of Conduct that all contributors are expected to follow. By participating, you are expected to uphold this code.

## Getting Started

1. Fork the repository on GitHub
2. Clone your fork to your local machine
   ```bash
   git clone https://github.com/YOUR-USERNAME/Paradigm.Enterprise.git
   cd Paradigm.Enterprise
   ```
3. Add the upstream repository
   ```bash
   git remote add upstream https://github.com/original-owner/Paradigm.Enterprise.git
   ```
4. Create a new branch for your changes
   ```bash
   git checkout -b feature/your-feature-name
   ```

## Development Workflow

1. Make sure your branch is up to date with the main branch
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```
2. Make your changes and commit them with clear, descriptive messages
   ```bash
   git commit -m "Add feature X"
   ```
3. Push your changes to your fork
   ```bash
   git push origin feature/your-feature-name
   ```

## Pull Request Process

1. Create a pull request from your fork to the main repository
2. Fill out the pull request template with:
   - Description of the changes
   - Related issue numbers
   - Testing performed
   - Documentation updates
3. Wait for code review feedback
4. Make requested changes if needed
5. Once approved, your PR will be merged

## RFC Process

For significant changes to the framework, we use a Request for Comments (RFC) process:

1. For any substantial change, create an RFC document following [our RFC template](../docs/rfc/template.md)
2. Submit the RFC as a pull request to get feedback
3. After discussion and approval, implement the changes

See the [RFC Process documentation](../docs/rfc/README.md) for detailed instructions.

## Coding Standards

- Follow the existing code style and patterns
- Use meaningful variable and function names
- Write clear comments for complex logic
- Keep methods focused and not too long
- Follow the principle of least surprise

## Testing

- All new code should include appropriate unit tests
- Ensure all tests pass before submitting a PR
- Maintain or improve the existing code coverage

To run tests:
```bash
./test.sh
```

## Documentation

- Update documentation for any changed functionality
- Use clear language and examples
- Keep API documentation up to date

## Issue Reporting

When reporting issues, please include:

1. A clear description of the problem
2. Steps to reproduce
3. Expected vs. actual behavior
4. Version information
5. Any relevant logs or screenshots

Thank you for contributing to the Paradigm Framework! 