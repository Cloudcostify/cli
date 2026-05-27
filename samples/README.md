# Sample Files

This directory contains sample files used for testing and demonstration purposes.

## Files

### Pulumi.yaml
Sample Pulumi project configuration file. This file defines the basic configuration for a Pulumi project.

### pulumi-preview.json
Sample output from a Pulumi preview command. This JSON file represents the infrastructure changes that would be made by Pulumi, which is then sent to the cost estimation API for analysis.

### cloudresources.json
Sample cloud resource configuration. This file demonstrates the structure of cloud resources used for cost estimation.

## Usage

These files are provided as examples and for testing purposes. When running the CLI in production:

1. Your actual Pulumi project will have its own `Pulumi.yaml` file
2. The preview JSON will be generated automatically by the Pulumi Automation API
3. The cloud resources JSON structure shows what kind of data the API expects

## Note

Do not use these files in production environments. They are for reference and testing only.
