# Contributing to Cloudcostify CLI

Thank you for your interest in contributing to Cloudcostify! We welcome contributions from the community and appreciate your help in making this tool better.

## Getting Started

### Prerequisites

- .NET 10.0 SDK or later
- Git
- A code editor (Visual Studio, VS Code, or Rider recommended)
- Pulumi CLI (for testing)

### Setting Up Your Development Environment

1. Fork the repository on GitHub
2. Clone your fork locally:
   ```bash
   git clone https://github.com/YOUR-USERNAME/cli.git
   cd cli
   ```

3. Add the upstream repository:
   ```bash
   git remote add upstream https://github.com/cloudcostify/cli.git
   ```

4. Create a new branch for your work:
   ```bash
   git checkout -b feature/your-feature-name
   ```

5. Restore dependencies:
   ```bash
   dotnet restore
   ```

6. Build the project:
   ```bash
   dotnet build
   ```

7. Run tests to ensure everything works:
   ```bash
   dotnet test
   ```

## Development Workflow

### Code Style

- We follow standard C# coding conventions
- Use the included `.editorconfig` file for consistent formatting
- Enable nullable reference types
- Write XML documentation comments for public APIs
- Keep methods focused and single-purpose

### Making Changes

1. Make your changes in your feature branch
2. Add or update tests for your changes
3. Ensure all tests pass: `dotnet test`
4. Build the project to check for warnings: `dotnet build`
5. Commit your changes with clear, descriptive commit messages

### Commit Messages

Follow these guidelines for commit messages:

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests when relevant

Example:
```
Add support for AWS Lambda cost estimation

- Implement Lambda resource parser
- Add pricing calculation for Lambda invocations
- Include tests for new functionality

Fixes #123
```

### Testing

- Write unit tests for all new functionality
- Ensure test coverage remains above 70%
- Use TUnit for test framework
- Use NSubstitute for mocking
- Use FluentAssertions for assertions

### Pull Request Process

1. Update the README.md with details of changes if applicable
2. Update the CHANGELOG.md with your changes
3. Ensure all tests pass and code builds without warnings
4. Push your branch to your fork
5. Submit a pull request to the main repository

### Pull Request Guidelines

- Fill in the pull request template completely
- Link any related issues
- Include screenshots for UI changes
- Describe your testing approach
- Keep pull requests focused on a single feature or fix

## Project Structure

```
src/
├── CostEstimationCli/           # Main CLI application
│   ├── Configuration/           # Configuration classes
│   ├── Models/                  # Data models
│   ├── Repositories/            # API communication
│   ├── Services/                # Business logic
│   └── Program.cs               # Entry point
└── CostEstimationCli.Tests/     # Unit tests
    ├── Repositories/
    └── Services/
```

## Areas for Contribution

We especially welcome contributions in these areas:

- **Additional Cloud Provider Support**: AWS, GCP, other providers
- **Resource Type Coverage**: More Azure/AWS/GCP resources
- **Performance Improvements**: Optimization and caching
- **Documentation**: Tutorials, examples, guides
- **Testing**: Improved test coverage
- **Bug Fixes**: Check the issues page

## Reporting Bugs

- Use the GitHub issue tracker
- Fill out the bug report template completely
- Include reproduction steps
- Provide error messages and logs
- Specify your environment (OS, .NET version, etc.)

## Feature Requests

- Use the GitHub issue tracker
- Fill out the feature request template
- Explain the use case and benefit
- Provide examples if possible

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inclusive experience for everyone. We expect all contributors to:

- Be respectful and considerate
- Welcome newcomers and help them learn
- Be patient and understanding
- Focus on what is best for the community

### Unacceptable Behavior

- Harassment, discrimination, or offensive comments
- Trolling or insulting/derogatory comments
- Personal or political attacks
- Publishing others' private information

## Questions?

- Join our Slack community: https://cloudcostify.slack.com
- Visit our website: https://cloudcostify.app/
- Open a discussion on GitHub

## License

By contributing to Cloudcostify CLI, you agree that your contributions will be licensed under the same license as the project (see LICENSE file).

## Recognition

Contributors will be recognized in:
- The project README
- Release notes for their contributions
- Our community hall of fame

Thank you for contributing to Cloudcostify! 🎉
