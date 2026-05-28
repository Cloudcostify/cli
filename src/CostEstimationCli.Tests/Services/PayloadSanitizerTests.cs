using System.Text.Json.Nodes;
using CostEstimationCli.Services.Sanitization;
using FluentAssertions;
using TUnit.Core;

namespace CostEstimationCli.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PayloadSanitizer"/>.
///
/// Three primary assertion groups, as required:
///   1. Raw JSON with sensitive production naming is converted to a completely
///      anonymous JSON graph (all corporate names / GUIDs / credentials gone).
///   2. Relationship links (parent IDs and dependency arrays) remain perfectly
///      unbroken — every edge in the sanitised document points to the same
///      anonymous token as the target resource's own "urn" field.
///   3. Key pricing metrics (SKU, Location, vCores, Count, vmSize) are exactly
///      identical before and after sanitisation.
/// </summary>
public class PayloadSanitizerTests
{
    // -------------------------------------------------------------------------
    // Shared test fixture — a realistic Pulumi preview document containing:
    //   • A Stack root resource
    //   • A ResourceGroup with a sensitive name
    //   • A VirtualMachine with credentials and an OS disk name (sensitive)
    //   • A SQL ManagedInstance with vCores / SKU / storageSizeInGB
    //   • A ManagedCluster (AKS) with an agent pool
    //
    // All resources share a single provider URN that embeds a GUID.
    // The VM, SQLMI and AKS all depend on the ResourceGroup.
    // -------------------------------------------------------------------------
    private const string SensitiveJson = """
    {
      "config": {
        "azure-native:location": "WestEurope"
      },
      "steps": [
        {
          "op": "create",
          "urn": "urn:pulumi:production::AcmeCorp::pulumi:pulumi:Stack::AcmeCorp-production",
          "newState": {
            "urn": "urn:pulumi:production::AcmeCorp::pulumi:pulumi:Stack::AcmeCorp-production",
            "custom": false,
            "type": "pulumi:pulumi:Stack"
          },
          "detailedDiff": null
        },
        {
          "op": "create",
          "urn": "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg",
          "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
          "newState": {
            "urn": "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg",
            "custom": true,
            "type": "azure-native:resources:ResourceGroup",
            "inputs": {
              "location": "WestEurope",
              "resourceGroupName": "customer-prod-rg-secret"
            },
            "parent": "urn:pulumi:production::AcmeCorp::pulumi:pulumi:Stack::AcmeCorp-production",
            "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "dependencies": [],
            "propertyDependencies": {}
          },
          "detailedDiff": null
        },
        {
          "op": "create",
          "urn": "urn:pulumi:production::AcmeCorp::azure-native:compute:VirtualMachine::customer-prod-vm",
          "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
          "newState": {
            "urn": "urn:pulumi:production::AcmeCorp::azure-native:compute:VirtualMachine::customer-prod-vm",
            "custom": true,
            "type": "azure-native:compute:VirtualMachine",
            "inputs": {
              "hardwareProfile": {
                "vmSize": "Standard_D2s_v3"
              },
              "location": "WestEurope",
              "osProfile": {
                "adminPassword": "SuperSecret!Prod123",
                "adminUsername": "prodadmin",
                "computerName": "customer-prod-vm",
                "linuxConfiguration": {
                  "provisionVMAgent": true,
                  "patchSettings": {
                    "assessmentMode": "ImageDefault"
                  }
                }
              },
              "priority": "Regular",
              "resourceGroupName": "customer-prod-rg-secret",
              "storageProfile": {
                "imageReference": {
                  "offer": "UbuntuServer",
                  "publisher": "Canonical",
                  "sku": "22_04-lts",
                  "version": "latest"
                },
                "osDisk": {
                  "createOption": "FromImage",
                  "managedDisk": {
                    "storageAccountType": "Standard_LRS"
                  },
                  "name": "customer-prod-vm-osdisk"
                }
              },
              "vmName": "customer-prod-vm"
            },
            "parent": "urn:pulumi:production::AcmeCorp::pulumi:pulumi:Stack::AcmeCorp-production",
            "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "dependencies": [
              "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg"
            ],
            "propertyDependencies": {
              "resourceGroupName": [
                "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg"
              ],
              "vmName": null
            }
          },
          "detailedDiff": null
        },
        {
          "op": "create",
          "urn": "urn:pulumi:production::AcmeCorp::azure-native:sql:ManagedInstance::customer-prod-sqlmi",
          "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
          "newState": {
            "urn": "urn:pulumi:production::AcmeCorp::azure-native:sql:ManagedInstance::customer-prod-sqlmi",
            "custom": true,
            "type": "azure-native:sql:ManagedInstance",
            "inputs": {
              "location": "WestEurope",
              "resourceGroupName": "customer-prod-rg-secret",
              "managedInstanceName": "customer-prod-sqlmi",
              "licenseType": "LicenseIncluded",
              "storageSizeInGB": 1024,
              "vCores": 8,
              "zoneRedundant": false,
              "sku": {
                "name": "GP_G8IM",
                "tier": "GeneralPurpose",
                "family": "G8IM",
                "capacity": 8
              }
            },
            "parent": "urn:pulumi:production::AcmeCorp::pulumi:pulumi:Stack::AcmeCorp-production",
            "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "dependencies": [
              "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg"
            ],
            "propertyDependencies": {
              "resourceGroupName": [
                "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg"
              ]
            }
          },
          "detailedDiff": null
        },
        {
          "op": "create",
          "urn": "urn:pulumi:production::AcmeCorp::azure-native:containerservice:ManagedCluster::customer-prod-aks",
          "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
          "newState": {
            "urn": "urn:pulumi:production::AcmeCorp::azure-native:containerservice:ManagedCluster::customer-prod-aks",
            "custom": true,
            "type": "azure-native:containerservice:ManagedCluster",
            "inputs": {
              "agentPoolProfiles": [
                {
                  "count": 5,
                  "name": "custprodpool",
                  "osType": "Linux",
                  "vmSize": "Standard_D4s_v3"
                }
              ],
              "kubernetesVersion": "1.28.0",
              "location": "WestEurope",
              "resourceGroupName": "customer-prod-rg-secret",
              "dnsPrefix": "customer-prod-aks-dns"
            },
            "parent": "urn:pulumi:production::AcmeCorp::pulumi:pulumi:Stack::AcmeCorp-production",
            "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "dependencies": [
              "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg"
            ],
            "propertyDependencies": {
              "resourceGroupName": [
                "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg"
              ]
            }
          },
          "detailedDiff": null
        },
        {
          "op": "create",
          "urn": "urn:pulumi:production::AcmeCorp::azure-native:compute:VirtualMachineScaleSet::customer-prod-spot-vmss",
          "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
          "newState": {
            "urn": "urn:pulumi:production::AcmeCorp::azure-native:compute:VirtualMachineScaleSet::customer-prod-spot-vmss",
            "custom": true,
            "type": "azure-native:compute:VirtualMachineScaleSet",
            "inputs": {
              "location": "WestEurope",
              "resourceGroupName": "customer-prod-rg-secret",
              "sku": {
                "capacity": 4,
                "name": "Standard_D4s_v3",
                "tier": "Standard"
              },
              "virtualMachineProfile": {
                "osProfile": {
                  "adminPassword": "SpotVmssSecret!99",
                  "adminUsername": "spotadmin",
                  "computerNamePrefix": "spot-vmss",
                  "linuxConfiguration": {
                    "provisionVMAgent": true
                  }
                },
                "priority": "Spot",
                "storageProfile": {
                  "imageReference": {
                    "offer": "UbuntuServer",
                    "publisher": "Canonical",
                    "sku": "22_04-lts",
                    "version": "latest"
                  },
                  "osDisk": {
                    "createOption": "FromImage",
                    "managedDisk": {
                      "storageAccountType": "Premium_LRS"
                    }
                  }
                }
              },
              "vmScaleSetName": "customer-prod-spot-vmss"
            },
            "parent": "urn:pulumi:production::AcmeCorp::pulumi:pulumi:Stack::AcmeCorp-production",
            "provider": "urn:pulumi:production::AcmeCorp::pulumi:providers:azure-native::default::a1b2c3d4-e5f6-7890-abcd-ef1234567890",
            "dependencies": [
              "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg"
            ],
            "propertyDependencies": {
              "location": null,
              "resourceGroupName": [
                "urn:pulumi:production::AcmeCorp::azure-native:resources:ResourceGroup::customer-prod-rg"
              ],
              "vmScaleSetName": null
            }
          },
          "detailedDiff": null
        }
      ]
    }
    """;

    // -------------------------------------------------------------------------
    // Helper to run the sanitizer and parse the result once per test
    // -------------------------------------------------------------------------
    private static (string raw, JsonObject sanitized, JsonArray steps) RunSanitizer()
    {
        var sanitizer = new PayloadSanitizer();
        var raw = sanitizer.Sanitize(SensitiveJson);
        var doc = JsonNode.Parse(raw)!.AsObject();
        return (raw, doc, doc["steps"]!.AsArray());
    }

    // =========================================================================
    // Group 1 — Sensitive data is completely removed
    // =========================================================================

    [Test]
    public void Sanitize_ShouldRemove_CorporateProjectName()
    {
        var (raw, _, _) = RunSanitizer();

        // Stack / project name must not appear anywhere in the output
        raw.Should().NotContain("AcmeCorp");
        raw.Should().NotContain("production");
    }

    [Test]
    public void Sanitize_ShouldRemove_SensitiveResourceNames()
    {
        var (raw, _, _) = RunSanitizer();

        raw.Should().NotContain("customer-prod");
        raw.Should().NotContain("customer-prod-rg-secret");
        raw.Should().NotContain("customer-prod-sqlmi");
        raw.Should().NotContain("customer-prod-aks-dns");
    }

    [Test]
    public void Sanitize_ShouldRemove_Credentials()
    {
        var (raw, _, _) = RunSanitizer();

        raw.Should().NotContain("SuperSecret");
        raw.Should().NotContain("prodadmin");
    }

    [Test]
    public void Sanitize_ShouldRemove_OriginalUrns()
    {
        var (raw, _, _) = RunSanitizer();

        // No original URN fragments must survive
        raw.Should().NotContain("urn:pulumi:production::AcmeCorp");
    }

    [Test]
    public void Sanitize_ShouldRemove_OriginalGuidFromProviderUrn()
    {
        var (raw, _, _) = RunSanitizer();

        // The GUID that was embedded in provider URNs must be gone
        raw.Should().NotContain("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    }

    [Test]
    public void Sanitize_ShouldRedact_ResourceGroupName()
    {
        var (_, _, steps) = RunSanitizer();

        // Every step that has a resourceGroupName in its inputs must show [redacted]
        foreach (var step in steps)
        {
            var inputs = step?["newState"]?["inputs"];
            if (inputs is null) continue;

            var rgName = inputs["resourceGroupName"];
            if (rgName is not null)
                rgName.GetValue<string>().Should().Be("[redacted]");
        }
    }

    [Test]
    public void Sanitize_ShouldRedact_OsDiskName()
    {
        var (_, _, steps) = RunSanitizer();

        var vmStep = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine");

        var osDiskName = vmStep!["newState"]!["inputs"]!["storageProfile"]!["osDisk"]!["name"];
        osDiskName!.GetValue<string>().Should().Be("[redacted]");
    }

    [Test]
    public void Sanitize_ShouldRedact_AgentPoolName()
    {
        var (_, _, steps) = RunSanitizer();

        var aksStep = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:containerservice:ManagedCluster");

        var poolName = aksStep!["newState"]!["inputs"]!["agentPoolProfiles"]!
            .AsArray()[0]!["name"];

        poolName!.GetValue<string>().Should().Be("[redacted]");
    }

    [Test]
    public void Sanitize_ShouldRedact_VmCredentials()
    {
        var (_, _, steps) = RunSanitizer();

        var vmStep = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine");

        var osProfile = vmStep!["newState"]!["inputs"]!["osProfile"]!;
        osProfile["adminPassword"]!.GetValue<string>().Should().Be("[redacted]");
        osProfile["adminUsername"]!.GetValue<string>().Should().Be("[redacted]");
        osProfile["computerName"]!.GetValue<string>().Should().Be("[redacted]");
    }

    [Test]
    public void Sanitize_AllUrnValues_ShouldMatchExpectedTokenPattern()
    {
        var (_, _, steps) = RunSanitizer();

        // Every "urn" field in the sanitised output must be an anonymous token
        // (stack-NN, provider-*-NN, or res-*-NN) — never an original URN.
        foreach (var step in steps)
        {
            var topUrn = step?["urn"]?.GetValue<string>();
            if (topUrn is not null)
                topUrn.Should().MatchRegex(@"^(stack|provider-|res-)");

            var innerUrn = step?["newState"]?["urn"]?.GetValue<string>();
            if (innerUrn is not null)
                innerUrn.Should().MatchRegex(@"^(stack|provider-|res-)");
        }
    }

    [Test]
    public void Sanitize_ShouldAnonymise_StandaloneGuid()
    {
        // Arrange — JSON with a field whose value is a bare subscription GUID
        const string json = """
        {
          "steps": [
            {
              "op": "create",
              "urn": "urn:pulumi:dev::TestProj::azure-native:resources:ResourceGroup::rg",
              "newState": {
                "urn": "urn:pulumi:dev::TestProj::azure-native:resources:ResourceGroup::rg",
                "custom": true,
                "type": "azure-native:resources:ResourceGroup",
                "inputs": {
                  "location": "westus",
                  "subscriptionId": "11111111-2222-3333-4444-555555555555"
                },
                "parent": "",
                "dependencies": []
              }
            }
          ]
        }
        """;

        var sanitizer = new PayloadSanitizer();
        var raw = sanitizer.Sanitize(json);

        raw.Should().NotContain("11111111-2222-3333-4444-555555555555",
            because: "standalone GUIDs must be replaced with generic tokens");
        raw.Should().Contain("00000000-0000-0000-0000-",
            because: "GUIDs must be replaced with generic zero-padded tokens");
    }

    // =========================================================================
    // Group 2 — Dependency graph edges remain intact
    // =========================================================================

    [Test]
    public void Sanitize_ResourceGroupParent_ShouldMatchStackUrn()
    {
        var (_, _, steps) = RunSanitizer();

        var stackUrn = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() == "pulumi:pulumi:Stack")!
            ["newState"]!["urn"]!.GetValue<string>();

        var rgParent = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:resources:ResourceGroup")!
            ["newState"]!["parent"]!.GetValue<string>();

        rgParent.Should().Be(stackUrn,
            because: "the ResourceGroup's parent edge must point to the Stack token");
    }

    [Test]
    public void Sanitize_VmDependencies_ShouldContainResourceGroupUrn()
    {
        var (_, _, steps) = RunSanitizer();

        var rgUrn = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:resources:ResourceGroup")!
            ["newState"]!["urn"]!.GetValue<string>();

        var vmDeps = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine")!
            ["newState"]!["dependencies"]!.AsArray();

        vmDeps.Select(d => d!.GetValue<string>())
            .Should().Contain(rgUrn,
                because: "the VM dependency edge must reference the ResourceGroup token");
    }

    [Test]
    public void Sanitize_SqlMiDependencies_ShouldContainResourceGroupUrn()
    {
        var (_, _, steps) = RunSanitizer();

        var rgUrn = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:resources:ResourceGroup")!
            ["newState"]!["urn"]!.GetValue<string>();

        var sqlDeps = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:sql:ManagedInstance")!
            ["newState"]!["dependencies"]!.AsArray();

        sqlDeps.Select(d => d!.GetValue<string>())
            .Should().Contain(rgUrn,
                because: "the SQLMI dependency edge must reference the ResourceGroup token");
    }

    [Test]
    public void Sanitize_AksDependencies_ShouldContainResourceGroupUrn()
    {
        var (_, _, steps) = RunSanitizer();

        var rgUrn = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:resources:ResourceGroup")!
            ["newState"]!["urn"]!.GetValue<string>();

        var aksDeps = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:containerservice:ManagedCluster")!
            ["newState"]!["dependencies"]!.AsArray();

        aksDeps.Select(d => d!.GetValue<string>())
            .Should().Contain(rgUrn,
                because: "the AKS dependency edge must reference the ResourceGroup token");
    }

    [Test]
    public void Sanitize_PropertyDependencies_ShouldMatchDependencyArrayEntries()
    {
        var (_, _, steps) = RunSanitizer();

        var rgUrn = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:resources:ResourceGroup")!
            ["newState"]!["urn"]!.GetValue<string>();

        // The VM's propertyDependencies["resourceGroupName"] should also point
        // at the same ResourceGroup token as the dependencies array.
        var vmPropDeps = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine")!
            ["newState"]!["propertyDependencies"]!
            ["resourceGroupName"]!.AsArray();

        vmPropDeps.Select(d => d!.GetValue<string>())
            .Should().Contain(rgUrn,
                because: "propertyDependencies must be re-mapped to the same token as dependencies");
    }

    [Test]
    public void Sanitize_TopLevelUrnAndNewStateUrn_ShouldBeIdentical()
    {
        var (_, _, steps) = RunSanitizer();

        // For every step the top-level "urn" and newState."urn" represent the
        // same resource; after sanitisation they must be the same token.
        foreach (var step in steps)
        {
            var topUrn   = step?["urn"]?.GetValue<string>();
            var innerUrn = step?["newState"]?["urn"]?.GetValue<string>();

            if (topUrn is not null && innerUrn is not null)
                topUrn.Should().Be(innerUrn,
                    because: "top-level urn and newState.urn must map to the same token");
        }
    }

    [Test]
    public void Sanitize_AllProviderFields_ShouldReferenceTheSameToken()
    {
        var (_, _, steps) = RunSanitizer();

        // All resources share one provider URN; after anonymisation every
        // "provider" field must resolve to the same single provider token.
        var providerTokens = steps
            .SelectMany(step => new[]
            {
                step?["provider"]?.GetValue<string>(),
                step?["newState"]?["provider"]?.GetValue<string>(),
            })
            .Where(v => v is not null)
            .Distinct()
            .ToList();

        providerTokens.Should().ContainSingle(
            because: "all steps share one provider — it must map to a single anonymous token");
    }

    [Test]
    public void Sanitize_EmptyParent_ShouldRemainEmpty()
    {
        const string json = """
        {
          "steps": [
            {
              "op": "create",
              "urn": "urn:pulumi:dev::Proj::azure-native:sql:Database::db",
              "newState": {
                "urn": "urn:pulumi:dev::Proj::azure-native:sql:Database::db",
                "custom": true,
                "type": "azure-native:sql:Database",
                "inputs": { "location": "westus" },
                "parent": "",
                "dependencies": []
              }
            }
          ]
        }
        """;

        var sanitizer = new PayloadSanitizer();
        var raw = sanitizer.Sanitize(json);
        var doc  = JsonNode.Parse(raw)!;
        var parent = doc["steps"]!.AsArray()[0]!["newState"]!["parent"]!.GetValue<string>();

        parent.Should().BeEmpty(
            because: "an empty parent string (no parent) must be preserved as-is");
    }

    [Test]
    public void Sanitize_DetailedDiff_ShouldBeStrippedEntirely()
    {
        var (_, _, steps) = RunSanitizer();

        // detailedDiff must be absent (not just null) — stripping it entirely
        // is the payload-bloat optimisation.
        foreach (var step in steps)
        {
            step!.AsObject().ContainsKey("detailedDiff").Should().BeFalse(
                because: "detailedDiff carries no pricing information and must be dropped");
        }
    }

    // =========================================================================
    // Group 3 — Pricing metadata is identical before and after sanitisation
    // =========================================================================

    [Test]
    public void Sanitize_ResourceType_ShouldBePreserved()
    {
        var original = JsonNode.Parse(SensitiveJson)!["steps"]!.AsArray();
        var (_, _, sanitizedSteps) = RunSanitizer();

        for (var i = 0; i < original.Count; i++)
        {
            var origType = original[i]!["newState"]!["type"]?.GetValue<string>();
            var saniType = sanitizedSteps[i]!["newState"]!["type"]?.GetValue<string>();

            if (origType is not null)
                saniType.Should().Be(origType,
                    because: $"resource type at step {i} must not change");
        }
    }

    [Test]
    public void Sanitize_Location_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        // Every resource that has a location must retain its original value.
        var locationsInSanitized = sanitizedSteps
            .Select(s => s!["newState"]!["inputs"]?["location"]?.GetValue<string>())
            .Where(v => v is not null)
            .ToList();

        locationsInSanitized.Should().AllBe("WestEurope",
            because: "location is a pricing signal and must be preserved unchanged");
    }

    [Test]
    public void Sanitize_SqlManagedInstance_VCores_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var sqlStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:sql:ManagedInstance");

        sqlStep!["newState"]!["inputs"]!["vCores"]!.GetValue<int>()
            .Should().Be(8, because: "vCores is a core pricing signal and must be preserved");
    }

    [Test]
    public void Sanitize_SqlManagedInstance_StorageSizeInGb_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var sqlStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:sql:ManagedInstance");

        sqlStep!["newState"]!["inputs"]!["storageSizeInGB"]!.GetValue<int>()
            .Should().Be(1024);
    }

    [Test]
    public void Sanitize_SqlManagedInstance_SkuHierarchy_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var sqlStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:sql:ManagedInstance");

        var sku = sqlStep!["newState"]!["inputs"]!["sku"]!.AsObject();

        sku["name"]!.GetValue<string>().Should().Be("GP_G8IM");
        sku["tier"]!.GetValue<string>().Should().Be("GeneralPurpose");
        sku["family"]!.GetValue<string>().Should().Be("G8IM");
        sku["capacity"]!.GetValue<int>().Should().Be(8);
    }

    [Test]
    public void Sanitize_SqlManagedInstance_LicenseType_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var sqlStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:sql:ManagedInstance");

        sqlStep!["newState"]!["inputs"]!["licenseType"]!.GetValue<string>()
            .Should().Be("LicenseIncluded");
    }

    [Test]
    public void Sanitize_SqlManagedInstance_ZoneRedundant_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var sqlStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:sql:ManagedInstance");

        sqlStep!["newState"]!["inputs"]!["zoneRedundant"]!.GetValue<bool>()
            .Should().BeFalse();
    }

    [Test]
    public void Sanitize_VirtualMachine_VmSize_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var vmStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine");

        vmStep!["newState"]!["inputs"]!["hardwareProfile"]!["vmSize"]!.GetValue<string>()
            .Should().Be("Standard_D2s_v3");
    }

    [Test]
    public void Sanitize_VirtualMachine_Priority_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var vmStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine");

        vmStep!["newState"]!["inputs"]!["priority"]!.GetValue<string>()
            .Should().Be("Regular");
    }

    [Test]
    public void Sanitize_VirtualMachine_OsImageReference_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var vmStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine");

        var imageRef = vmStep!["newState"]!["inputs"]!["storageProfile"]!["imageReference"]!.AsObject();

        imageRef["offer"]!.GetValue<string>().Should().Be("UbuntuServer");
        imageRef["publisher"]!.GetValue<string>().Should().Be("Canonical");
        imageRef["sku"]!.GetValue<string>().Should().Be("22_04-lts");
        imageRef["version"]!.GetValue<string>().Should().Be("latest");
    }

    [Test]
    public void Sanitize_VirtualMachine_ManagedDiskStorageType_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var vmStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine");

        var storageType = vmStep!["newState"]!["inputs"]!["storageProfile"]!["osDisk"]!
            ["managedDisk"]!["storageAccountType"]!.GetValue<string>();

        storageType.Should().Be("Standard_LRS");
    }

    [Test]
    public void Sanitize_VirtualMachine_LinuxConfigurationBlock_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var vmStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachine");

        var linuxCfg = vmStep!["newState"]!["inputs"]!["osProfile"]!["linuxConfiguration"]!;

        // provisionVMAgent is a pricing-relevant OS signal
        linuxCfg["provisionVMAgent"]!.GetValue<bool>().Should().BeTrue();

        // patchSettings / assessmentMode are also whitelisted
        linuxCfg["patchSettings"]!["assessmentMode"]!.GetValue<string>()
            .Should().Be("ImageDefault");
    }

    [Test]
    public void Sanitize_Aks_AgentPoolPricingSignals_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var aksStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:containerservice:ManagedCluster");

        var pool = aksStep!["newState"]!["inputs"]!["agentPoolProfiles"]!.AsArray()[0]!.AsObject();

        pool["count"]!.GetValue<int>().Should().Be(5);
        pool["osType"]!.GetValue<string>().Should().Be("Linux");
        pool["vmSize"]!.GetValue<string>().Should().Be("Standard_D4s_v3");
        pool["kubernetesVersion"].Should().BeNull(
            because: "kubernetesVersion lives on the cluster input, not inside agentPoolProfiles");
    }

    [Test]
    public void Sanitize_Aks_KubernetesVersion_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var aksStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:containerservice:ManagedCluster");

        aksStep!["newState"]!["inputs"]!["kubernetesVersion"]!.GetValue<string>()
            .Should().Be("1.28.0");
    }

    [Test]
    public void Sanitize_Op_ShouldBePreservedOnAllSteps()
    {
        var original = JsonNode.Parse(SensitiveJson)!["steps"]!.AsArray();
        var (_, _, sanitizedSteps) = RunSanitizer();

        for (var i = 0; i < original.Count; i++)
        {
            var origOp = original[i]!["op"]?.GetValue<string>();
            var saniOp = sanitizedSteps[i]!["op"]?.GetValue<string>();

            saniOp.Should().Be(origOp,
                because: $"'op' at step {i} is a structural flag and must not change");
        }
    }

    [Test]
    public void Sanitize_Custom_FlagShouldBePreservedOnAllSteps()
    {
        var original = JsonNode.Parse(SensitiveJson)!["steps"]!.AsArray();
        var (_, _, sanitizedSteps) = RunSanitizer();

        for (var i = 0; i < original.Count; i++)
        {
            var origCustom = original[i]!["newState"]?["custom"]?.GetValue<bool>();
            var saniCustom = sanitizedSteps[i]!["newState"]?["custom"]?.GetValue<bool>();

            saniCustom.Should().Be(origCustom,
                because: $"'custom' flag at step {i} is informational and must not change");
        }
    }

    // =========================================================================
    // Group 4 — Token format and determinism
    // =========================================================================

    [Test]
    public void Sanitize_StackResource_ShouldReceiveStackPrefixedToken()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var stackUrn = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() == "pulumi:pulumi:Stack")!
            ["newState"]!["urn"]!.GetValue<string>();

        stackUrn.Should().StartWith("stack-",
            because: "pulumi:pulumi:Stack resources must use the 'stack-NN' token format");
    }

    [Test]
    public void Sanitize_ProviderUrn_ShouldReceiveProviderPrefixedToken()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var providerToken = sanitizedSteps
            .First(s => s!["provider"] is not null)!
            ["provider"]!.GetValue<string>();

        providerToken.Should().StartWith("provider-",
            because: "Pulumi provider URNs must use the 'provider-*-NN' token format");
    }

    [Test]
    public void Sanitize_ResourceGroup_ShouldReceiveResPrefixedToken()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var rgUrn = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:resources:ResourceGroup")!
            ["newState"]!["urn"]!.GetValue<string>();

        rgUrn.Should().StartWith("res-",
            because: "cloud resources must use the 'res-*-NN' token format");
        rgUrn.Should().Contain("resources-resourcegroup",
            because: "the type slug must be derived from the resource type");
    }

    [Test]
    public void Sanitize_CalledTwiceWithSameInput_ShouldProduceSameOutput()
    {
        var sanitizer = new PayloadSanitizer();

        var first  = sanitizer.Sanitize(SensitiveJson);
        var second = sanitizer.Sanitize(SensitiveJson);

        first.Should().Be(second,
            because: "sanitisation must be deterministic for the same input");
    }

    // =========================================================================
    // Group 5 — Input validation
    // =========================================================================

    [Test]
    public void Sanitize_WithNullInput_ShouldThrowArgumentException()
    {
        var sanitizer = new PayloadSanitizer();

        var act = () => sanitizer.Sanitize(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Sanitize_WithEmptyInput_ShouldThrowArgumentException()
    {
        var sanitizer = new PayloadSanitizer();

        var act = () => sanitizer.Sanitize(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Sanitize_WithWhitespaceInput_ShouldThrowArgumentException()
    {
        var sanitizer = new PayloadSanitizer();

        var act = () => sanitizer.Sanitize("   ");

        act.Should().Throw<ArgumentException>();
    }

    // =========================================================================
    // Group 6 — VM/VMSS priority preservation
    // =========================================================================

    [Test]
    public void Sanitize_VirtualMachineScaleSet_Priority_ShouldBePreserved()
    {
        var (_, _, sanitizedSteps) = RunSanitizer();

        var vmssStep = sanitizedSteps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachineScaleSet");

        var priority = vmssStep!["newState"]!["inputs"]!["virtualMachineProfile"]!["priority"]!
            .GetValue<string>();

        priority.Should().Be("Spot",
            because: "VMSS priority lives inside virtualMachineProfile and must be preserved as-is");
    }

    [Test]
    public void Sanitize_VirtualMachine_Priority_IsPreserved_WhenNestedInsideInputs()
    {
        // Stand-alone test using minimal JSON — confirms priority at inputs level
        const string json = """
        {
          "steps": [
            {
              "op": "create",
              "urn": "urn:pulumi:dev::Proj::azure-native:compute:VirtualMachine::spot-vm",
              "newState": {
                "urn": "urn:pulumi:dev::Proj::azure-native:compute:VirtualMachine::spot-vm",
                "custom": true,
                "type": "azure-native:compute:VirtualMachine",
                "inputs": {
                  "hardwareProfile": { "vmSize": "Standard_D2s_v3" },
                  "location": "eastus",
                  "priority": "Spot",
                  "resourceGroupName": "my-rg"
                },
                "parent": "",
                "dependencies": []
              }
            }
          ]
        }
        """;

        var sanitizer = new PayloadSanitizer();
        var raw = sanitizer.Sanitize(json);
        var doc = JsonNode.Parse(raw)!;
        var priority = doc["steps"]!.AsArray()[0]!["newState"]!["inputs"]!["priority"]!
            .GetValue<string>();

        priority.Should().Be("Spot",
            because: "VM priority at the inputs level must survive sanitisation unchanged");
    }

    // =========================================================================
    // Group 7 — Payload bloat reduction
    // =========================================================================

    [Test]
    public void Sanitize_PropertyDependencies_NullEntries_ShouldBeStripped()
    {
        var (_, _, steps) = RunSanitizer();

        // The VMSS step has null propertyDependency entries; they must be absent.
        var vmssStep = steps
            .First(s => s!["newState"]!["type"]!.GetValue<string>() ==
                        "azure-native:compute:VirtualMachineScaleSet");

        var propDeps = vmssStep!["newState"]!["propertyDependencies"]!.AsObject();

        // "location" and "vmScaleSetName" had null values in the source — they
        // must be gone entirely from the sanitised output.
        propDeps.ContainsKey("location").Should().BeFalse(
            because: "null propertyDependency entries add bloat without carrying graph edges");
        propDeps.ContainsKey("vmScaleSetName").Should().BeFalse(
            because: "null propertyDependency entries add bloat without carrying graph edges");

        // The non-null resourceGroupName entry must still be present.
        propDeps.ContainsKey("resourceGroupName").Should().BeTrue(
            because: "non-null propertyDependency entries carry real graph edges and must be kept");
    }

    [Test]
    public void Sanitize_OutputShouldBeSmallerThan_InputWithLargeDetailedDiff()
    {
        // Build a payload where detailedDiff carries a large non-null object.
        const string json = """
        {
          "steps": [
            {
              "op": "update",
              "urn": "urn:pulumi:dev::Proj::azure-native:compute:VirtualMachine::vm",
              "newState": {
                "urn": "urn:pulumi:dev::Proj::azure-native:compute:VirtualMachine::vm",
                "custom": true,
                "type": "azure-native:compute:VirtualMachine",
                "inputs": {
                  "hardwareProfile": { "vmSize": "Standard_D4s_v3" },
                  "location": "westeurope",
                  "priority": "Regular",
                  "resourceGroupName": "my-rg"
                },
                "parent": "",
                "dependencies": []
              },
              "detailedDiff": {
                "hardwareProfile.vmSize": {
                  "kind": "UPDATE",
                  "inputDiff": { "from": "Standard_D2s_v3", "to": "Standard_D4s_v3" },
                  "outputDiff": { "from": "Standard_D2s_v3", "to": "Standard_D4s_v3" }
                }
              }
            }
          ]
        }
        """;

        var sanitizer = new PayloadSanitizer();
        var sanitized = sanitizer.Sanitize(json);

        sanitized.Length.Should().BeLessThan(json.Length,
            because: "stripping detailedDiff must reduce the payload size");

        sanitized.Should().NotContain("detailedDiff",
            because: "detailedDiff must be completely absent from the sanitised output");
        sanitized.Should().NotContain("hardwareProfile.vmSize",
            because: "diff telemetry must not leak into the sanitised output");
    }
}
