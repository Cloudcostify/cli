# Changelog

All notable changes to the Cloudcostify CLI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure with .NET 10
- Dependency injection architecture
- Configuration management with appsettings.json
- Service layer for business logic separation
- Repository pattern for API communication
- Comprehensive unit test suite with TUnit
- GitHub Actions CI/CD pipeline
- Support for Azure Kubernetes Service (AKS) cost estimation
- Spectre.Console for rich terminal output
- Environment variable configuration support
- Azure DevOps pipeline detection
- Logging with Microsoft.Extensions.Logging

### Changed
- Migrated from flat structure to layered architecture
- Refactored from static methods to dependency injection
- Updated to .NET 10 from .NET 7/9
- Improved error handling and logging
- Enhanced configuration management

### Fixed
- Logic errors in environment variable validation (original lines 55, 64-67, 74, 78)
- Proper null checking for configuration values
- Corrected API key validation flow

## [1.0.0] - TBD

### Added
- First stable release
- Pulumi integration via Automation API
- Cost estimation API integration
- Support for Azure resources:
  - AKS Managed Clusters
  - Virtual Machine SKUs
  - Node Pools
- Support for AWS resources (limited)
- CI/CD integration (Azure DevOps, GitHub Actions)
- Configurable authentication
- Detailed cost breakdowns by time period:
  - Per hour, day, week, month, quarter, year
- Rich console output with tables and formatting
- Comprehensive documentation

### Known Limitations
- Limited AWS resource support
- No GCP support yet
- Requires Pulumi Automation API access
- Windows-focused (tested on Windows primarily)

---

## Version History

### Release Notes Guidelines

Each release should include:
- **Added**: New features
- **Changed**: Changes in existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Security vulnerability fixes

---

For more information, visit: https://cloudcostify.app/
