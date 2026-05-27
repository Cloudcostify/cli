---
name: Bug Report
about: Create a report to help us improve
title: '[BUG] '
labels: bug
assignees: ''
---

## Bug Description

A clear and concise description of what the bug is.

## Steps to Reproduce

1. Go to '...'
2. Run command '...'
3. See error

## Expected Behavior

A clear and concise description of what you expected to happen.

## Actual Behavior

A clear and concise description of what actually happened.

## Error Messages

```
Paste any error messages or stack traces here
```

## Environment

- **OS**: [e.g., Windows 11, Ubuntu 22.04]
- **.NET Version**: [e.g., .NET 10.0]
- **CLI Version**: [e.g., 1.0.0]
- **Pulumi Version**: [e.g., 3.54.1]
- **Cloud Provider**: [e.g., Azure, AWS]

## Configuration

**appsettings.json** (remove sensitive data):
```json
{
  "CostEstimation": {
    "BaseUrl": "...",
    "Authentication": {
      "Enabled": false
    }
  }
}
```

**Environment Variables** (remove sensitive data):
- CLOUDCOSTIFY_BASE_URL: ...
- CLOUDCOSTIFY_PULUMI_PROJECT_STACK_NAME: ...

## Logs

```
Paste relevant log output here
```

## Additional Context

Add any other context about the problem here, such as:
- Screenshots
- Sample Pulumi code
- API responses
- Related issues

## Possible Solution

If you have suggestions on how to fix the bug, please share them here.
